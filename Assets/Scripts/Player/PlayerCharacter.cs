using UnityEngine;
using Fusion;
using UnityEngine.Rendering;
using Starter.Shooter;
using LichLord.Projectiles;
using LichLord.NonPlayerCharacters;
using LichLord.World;
using LichLord.Props;
using System.Collections.Generic;
using System.IO;
using LichLord.Buildables;

namespace LichLord
{
    public class PlayerCharacter : RelayPlayer, INetActor, IHitInstigator, IHitTarget, IChunkTrackable
    {
        [Header("References")]
        public Health Health;
        public PlayerCharacterMovementComponent Movement;
        public PlayerCameraController CameraController;
        public PlayerCharacterInput Input;
        public ManeuverComponent Maneuvers;
        public PlayerProjectilePool ProjectilePool;
        public PlayerCurrencyComponent Currency;
        public HurtboxComponent Hurtbox;
        public Animator Animator;
        public BuilderComponent Builder;
        public PlayerCharacterFSM FSM;

        [SerializeField] private MuzzleComponent _muzzleComponent;
        public MuzzleComponent Muzzle => _muzzleComponent;

        public Renderer[] HeadRenderers;
        public GameObject[] FirstPersonOverlayObjects;


        [Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
        public string Nickname { get; set; }

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

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

        // Cached list of Chunks for current and neighboring chunks
        private List<Chunk> _cachedChunks = new List<Chunk>();
        public IReadOnlyList<Chunk> CachedChunks => _cachedChunks.AsReadOnly();

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

            Runner.SetPlayerObject(Runner.LocalPlayer, Object);
            Runner.SetPlayerAlwaysInterested(Runner.LocalPlayer, Object, true);

            if (HasStateAuthority)
            {
                Nickname = "Steve " + GetProjectName();
                Context.LocalPlayerCharacter = this;
                Context.LocalPlayerRef = Object.StateAuthority;
                Input.OnSpawned();
            }

            Movement.OnSpawned();

            Context.NetworkGame.OnPlayerSpawned(this);

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

            // Initialize cached prop states
            UpdateChunk();
        }

        public void ApplySpawnParameters(Vector3 position, Quaternion rotation, EMovementState moveState)
        {
            Movement.CC.Move(position);
            Movement.SetMovementState(moveState);
            Input.SetLookRotation(rotation);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            Context.NetworkGame.OnPlayerDespawned(this);
            //Context.WorldSaveLoadManager.OnPlayerDespawned(this);

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


        private void Respawn(Vector3 position)
        {
            Health.Revive();
        }

        private void OnNicknameChanged()
        {
            gameObject.name = Nickname;

            if (HasStateAuthority)
                return; // Do not show nickname for local player

        }

        void IHitInstigator.HitPerformed(ref FHitUtilityData hit)
        {
            if (hit.target is NonPlayerCharacter npc)
            {
                int currentAnimIndex = npc.State.CurrentAnimIndex;
                int hitReactIndex = UnityEngine.Random.Range(0, 4);

                // If the new index is the same as the current, increment and wrap around
                if (hitReactIndex == currentAnimIndex)
                {
                    hitReactIndex = (currentAnimIndex + 1) % 4;
                }

                npc.Replicator.RPC_DealDamageToNPC(npc.Index, hit.damageData.damageValue, hitReactIndex);

                if (!Runner.IsSharedModeMasterClient)
                    npc.Replicator.Predict_DealDamageToNPC(npc.Index, hit.damageData.damageValue, hitReactIndex);
            }

            if (hit.target is Prop prop)
            {
                Context.PropManager.RPC_DealDamageToProp(prop.RuntimeState.guid, hit.damageData.damageValue);

                if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                    Context.PropManager.Predict_DealDamageToProp(prop.RuntimeState.guid, hit.damageData.damageValue);
            }
        }

        void INetActor.ProjectileSpawnedCallback(Projectile projectile, ProjectileDefinition definition, ref FProjectileData data)
        {
            //Debug.Log("FireTick: " + data.FireTick + ", CurrentTick: " + Context.Runner.Tick);
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

                // Update cached nearby chunks
                _cachedChunks = Context.ChunkManager.GetNearbyChunks(CurrentChunk.ChunkID, radius: 2);

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

            _cachedPropStates.Clear();

            HashSet<int> newGuids = new HashSet<int>();
            HashSet<int> oldGuids = _visibileGuids;
            foreach (Chunk chunk in _cachedChunks)
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

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_TakeProjectileHit(int projectileIndex, int damage)
        { 
            Debug.Log("Projectile Hit me: " +  projectileIndex + " Damage: " + damage);

           // Context.ProjectileManager.Server
        }

        private string GetProjectName()
        {
            // Extract project name from Application.dataPath (e.g., "C:/Projects/MyGame_clone_0/Assets")
            string path = Application.dataPath;
            string projectName = Path.GetFileName(Path.GetDirectoryName(path));
            return string.IsNullOrEmpty(projectName) ? "DefaultInstance" : projectName;
        }
    }
}