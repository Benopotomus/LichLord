using UnityEngine;
using Fusion;
using UnityEngine.Rendering;
using Starter.Shooter;
using Starter;
using LichLord.Projectiles;
using SoulGames.EasyGridBuilderPro;
using LichLord.NonPlayerCharacters;
using LichLord.World;
using LichLord.Props;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LichLord
{
    public class PlayerCharacter : RelayPlayer, INetActor, IHitInstigator, IHitTarget, IChunkTrackable
    {
        [Header("References")]
        public Health Health;
        public PlayerCharacterMovementComponent Movement;
        public PlayerCameraController CameraController;
        public PlayerCharacterInput Input;
        public CreatureManeuvers Actions;
        public PlayerProjectilePool ProjectilePool;
        public HurtboxComponent Hurtbox;
        public Animator Animator;
        public Transform CameraPivot;
        public UINameplate Nameplate;
        public Renderer[] HeadRenderers;
        public GameObject[] FirstPersonOverlayObjects;

        [Header("Animation Setup")]
        public Transform ChestTargetPosition;
        public Transform ChestBone;

        [Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
        public string Nickname { get; set; }

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        public FNetObjectID NetObjectID
        {
            get => Object != null ? new FNetObjectID { networkId = Object.Id } : default;
        }

        INetActor IHitInstigator.NetActor => this;

        private Chunk _chunk;
        public Chunk CurrentChunk { get { return _chunk; } set { _chunk = value; } }
        public Vector3 Position => CachedTransform.position;
        public bool IsAttackable { get { return true; } }

        // Cached list of PropRuntimeState for current and neighboring chunks
        private List<PropRuntimeState> _cachedPropStates = new List<PropRuntimeState>();
        public IReadOnlyList<PropRuntimeState> CachedPropStates => _cachedPropStates.AsReadOnly();

        [SerializeField]
        private BuildableGridObjectTypeSO item;

        public static bool TryGetLocalPlayer(NetworkRunner runner, out PlayerCharacter playerCreature)
        {
            playerCreature = null;

            if (runner == null)
                return false;

            runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject playerObject);

            if (playerObject == null)
                return false;

            playerCreature = playerObject.GetComponent<PlayerCharacter>();
            if (playerCreature == null)
                return false;

            return true;
        }

        public override void Spawned()
        {
            base.Spawned();

            Context.LocalPlayerCreature = this;
            Context.LocalPlayerRef = Object.StateAuthority;
            Context.NetworkGame.OnPlayerSpawned(this);

            Runner.SetPlayerObject(Runner.LocalPlayer, Object);
            Runner.SetPlayerAlwaysInterested(Runner.LocalPlayer, Object, true);

            // In case the nickname is already changed, trigger the change manually
            OnNicknameChanged();

            if (HasStateAuthority)
            {
                // For input authority, deactivate head renderers so they don’t obstruct the view
                for (int i = 0; i < HeadRenderers.Length; i++)
                {
                    HeadRenderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }

                // Some objects (e.g., weapon) are rendered with a secondary Overlay camera
                int overlayLayer = LayerMask.NameToLayer("FirstPersonOverlay");
                for (int i = 0; i < FirstPersonOverlayObjects.Length; i++)
                {
                    FirstPersonOverlayObjects[i].layer = overlayLayer;
                }
            }

            // Ensure ActionManager is assigned
            if (Actions == null)
            {
                Actions = GetComponent<CreatureManeuvers>();
                if (Actions == null)
                    Debug.LogError("[PlayerCharacter] Missing ActionManager component.");
            }

            // Initialize cached prop states
            UpdateChunk(Context.ChunkManager);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            Context.NetworkGame.OnPlayerDespawned(this);

            if(CurrentChunk != null)
                CurrentChunk.RemoveObject(this);
        }

        public override void Render()
        {
            base.Render();
            // Disable hits when player is dead
            Hurtbox.enabled = Health.IsAlive;

            // Change the chunk and tell the server we've changed chunks
            UpdateChunk(Context.ChunkManager);

            // Update 
        }

        private void LateUpdate()
        {
            if (Health.IsAlive == false)
                return;

            // IK after animations
            var pitchRotation = Movement.WorldTransform.Pitch;
            CameraPivot.localRotation = Quaternion.Euler(pitchRotation, 0, 0);

            // Dummy IK solution: snap chest bone to ChestTargetPosition
            float blendAmount = HasStateAuthority ? 0.05f : 0.2f;
            ChestBone.position = Vector3.Lerp(ChestTargetPosition.position, ChestBone.position, blendAmount);
            ChestBone.rotation = Quaternion.Lerp(ChestTargetPosition.rotation, ChestBone.rotation, blendAmount);
        }

        private void Respawn(Vector3 position)
        {
            Health.Revive();
        }

        private void OnNicknameChanged()
        {
            if (HasStateAuthority)
                return; // Do not show nickname for local player

            Nameplate.SetNickname(Nickname);
        }

        void IHitInstigator.HitPerformed(ref FHitUtilityData hit)
        {
            if (hit.target is NonPlayerCharacter npc)
            {
                npc.Replicator.RPC_DealDamageToNPC(npc.GUID, hit.damageData.damageValue);

                if (!Runner.IsSharedModeMasterClient)
                    npc.Replicator.Predict_DealDamageToNPC(npc.GUID, hit.damageData.damageValue);
            }

            if (hit.target is Prop prop)
            {
                Context.PropManager.RPC_DealDamageToProp(prop.RuntimeState.guid, hit.damageData.damageValue);

                if (!Runner.IsSharedModeMasterClient)
                    Context.PropManager.Predict_DealDamageToProp(prop.RuntimeState.guid, hit.damageData.damageValue);
            }
        }

        void IHitTarget.ProcessHit(ref FHitUtilityData hit)
        {
        }

        void IHitTarget.OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public void UpdateChunk(ChunkManager chunkManager)
        {
            var lastChunk = CurrentChunk;
            var newChunk = chunkManager.GetChunkAtPosition(CachedTransform.position);

            if (lastChunk != newChunk)
            {
                CurrentChunk = newChunk;

                if (lastChunk != null)
                    lastChunk.RemoveObject(this);

                if (newChunk != null)
                    newChunk.AddObject(this);

                // Update cached prop states on chunk change
                UpdateVisibilePropStates(chunkManager);
                Debug.Log($"Player chunk changed from {lastChunk?.ChunkID} to {newChunk?.ChunkID}. Cached {CachedPropStates.Count} prop states.", this);
            }
        }

        public HashSet<int> _visibileGuids = new HashSet<int>();

        private void UpdateVisibilePropStates(ChunkManager chunkManager)
        {
            if (CurrentChunk == null)
                return;

            // Get current and neighboring chunks (radius = 1)
            List<Chunk> nearbyChunks = chunkManager.GetNearbyChunks(CurrentChunk.ChunkID, radius: 1);
            _cachedPropStates.Clear();

            HashSet<int> newGuids = new HashSet<int>();
            HashSet<int> oldGuids = _visibileGuids;
            foreach (Chunk chunk in nearbyChunks)
            {
                if (chunk == null || chunk.PropStates == null)
                    continue;

                foreach (PropRuntimeState state in chunk.PropStates)
                {
                    if (state != null && !_cachedPropStates.Contains(state))
                    {
                        _cachedPropStates.Add(state);
                        _visibileGuids.Add(state.guid);
                        newGuids.Add(state.guid);
                    }
                }
            }

            // Despawn props no longer in cached states
            DespawnUnusedProps(newGuids, oldGuids);

            _visibileGuids = newGuids;
        }

        private void DespawnUnusedProps(HashSet<int> newGuids, HashSet<int> oldGuids)
        {
            // Identify GUIDs to despawn (in previous set but not in current set)
            List<int> guidsToDespawn = new List<int>();
            foreach (int guid in oldGuids)
            {
                if (!newGuids.Contains(guid))
                {
                    guidsToDespawn.Add(guid);
                }
            }

            // Despawn each prop
            foreach (int guid in guidsToDespawn)
            {
                Context.PropManager.DespawnProp(guid);
            }
        }


    }
}