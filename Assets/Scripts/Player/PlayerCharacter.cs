using UnityEngine;
using Fusion;
using UnityEngine.Rendering;
using Starter.Shooter;
using Starter;
using LichLord.Projectiles;
using LichLord.NonPlayerCharacters;
using LichLord.World;
using LichLord.Props;
using System.Collections.Generic;

namespace LichLord
{
    public class PlayerCharacter : RelayPlayer, INetActor, IHitInstigator, IHitTarget, IChunkTrackable
    {
        [Header("References")]
        public Health Health;
        public PlayerCharacterMovementComponent Movement;
        public PlayerCameraController CameraController;
        public PlayerCharacterInput Input;
        public PlayerCharacterManeuvers Maneuvers;
        public PlayerProjectilePool ProjectilePool;
        public HurtboxComponent Hurtbox;
        public Animator Animator;

        public Renderer[] HeadRenderers;
        public GameObject[] FirstPersonOverlayObjects;

        [Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
        public string Nickname { get; set; }

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        [SerializeField] private Transform _handBoneLeft;
        [SerializeField] private Transform _handBoneRight;

        public FNetObjectID NetObjectID
        {
            get => Object != null ? new FNetObjectID { networkId = Object.Id } : default;
        }

        INetActor IHitInstigator.NetActor => this;

        // IChunkTrackable
        private Chunk _chunk;
        public Chunk CurrentChunk { get { return _chunk; } set { _chunk = value; } }
        public Vector3 Position => CachedTransform.position;
        public bool IsAttackable { get { return true; } }

        // Cached list of PropRuntimeState for current and neighboring chunks
        private List<PropRuntimeState> _cachedPropStates = new List<PropRuntimeState>();
        public IReadOnlyList<PropRuntimeState> CachedPropStates => _cachedPropStates.AsReadOnly();

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

            if (HasStateAuthority)
            {
                Context.LocalPlayerCharacter = this;
                Context.LocalPlayerRef = Object.StateAuthority;
            }

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
            if (Maneuvers == null)
            {
                Maneuvers = GetComponent<PlayerCharacterManeuvers>();
                if (Maneuvers == null)
                    Debug.LogError("[PlayerCharacter] Missing ActionManager component.");
            }

            // Initialize cached prop states
            UpdateChunk();
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
            UpdateChunk();
        }

        private Quaternion _lastRotation; // Store the original rotation
        private bool wasUpperBodyActive;         // Track state to detect transitions


        private void Respawn(Vector3 position)
        {
            Health.Revive();
        }

        private void OnNicknameChanged()
        {
            if (HasStateAuthority)
                return; // Do not show nickname for local player

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

        public void UpdateChunk()
        {
            var lastChunk = CurrentChunk;
            var newChunk = Context.ChunkManager.GetChunkAtPosition(CachedTransform.position);

            if (lastChunk != newChunk)
            {
                CurrentChunk = newChunk;

                if (lastChunk != null)
                    lastChunk.RemoveObject(this);

                if (newChunk != null)
                    newChunk.AddObject(this);

                // Update cached prop states on chunk change
                UpdateVisibilePropStates();
                //Debug.Log($"Player chunk changed from ({lastChunk?.ChunkID.X}, {lastChunk?.ChunkID.Y}) to ({newChunk?.ChunkID.X}, {newChunk?.ChunkID.Y}). Cached {CachedPropStates.Count} prop states.", this);
            }
        }

        public HashSet<int> _visibileGuids = new HashSet<int>();

        public void UpdateVisibilePropStates()
        {
            if (CurrentChunk == null)
                return;

            // Get current and neighboring chunks (radius = 1)
            List<Chunk> nearbyChunks = Context.ChunkManager.GetNearbyChunks(CurrentChunk.ChunkID, radius: 2);
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

        public Vector3 GetMuzzlePosition(EMuzzle muzzle)
        {
            switch (muzzle)
            {
                case EMuzzle.LeftHand:
                    return _handBoneLeft.position;

                case EMuzzle.RightHand:
                    return _handBoneLeft.position;

                case EMuzzle.LeftHand_RightHand_Blend:
                    return Vector3.Lerp(_handBoneLeft.position, _handBoneRight.position, 0.5f); 
            }

            return _cachedTransform.position;
        }

    }
}