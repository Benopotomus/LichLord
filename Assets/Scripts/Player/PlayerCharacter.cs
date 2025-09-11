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
        public InteractorComponent Interactor;
        public PlayerCharacterAimComponent Aim;
        public PlayerNexusComponent Nexus;
        public PlayerHealthComponent Health;

        [SerializeField] private PlayerCharacterAnimationController _animationController;
        public PlayerCharacterAnimationController AnimationController => _animationController;

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

        public float BonusRadius { get { return 0; } }

        INetActor IHitInstigator.NetActor => this;

        // IChunkTrackable
        private Chunk _chunk;
        public Chunk CurrentChunk { get { return _chunk; } set { _chunk = value; } }
        public Vector3 Position => CachedTransform.position;
        public bool IsAttackable { get { return true; } }
        public virtual Collider HurtBoxCollider { get { return Hurtbox.HurtBoxes[0]; } }

        // Cached list of PropRuntimeState for current and neighboring chunks
        private List<PropRuntimeState> _cachedPropStates = new List<PropRuntimeState>();
        public IReadOnlyList<PropRuntimeState> CachedPropStates => _cachedPropStates.AsReadOnly();

        // Cached list of Chunks for current and neighboring chunks
        private List<Chunk> _cachedChunks = new List<Chunk>();
        public IReadOnlyList<Chunk> CachedChunks => _cachedChunks.AsReadOnly();

        [Networked]
        [SerializeField]
        public int PlayerIndex { get; set; }

        public bool SpawnComplete = false;

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

                PlayerIndex = Context.NetworkGame.GetFreePlayerIndex();
            }

            SpawnComplete = true;

        }

        public void ApplySpawnParameters(Vector3 position, Quaternion rotation, EMovementState moveState, string nickName)
        {
            Nickname = nickName;
            transform.position = position;
            Movement.SetMovementState(moveState);
            Input.SetLookRotation(rotation);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            SpawnComplete = false;
            Context.NetworkGame.OnPlayerDespawned(this);
            //Context.WorldSaveLoadManager.OnPlayerDespawned(this);

            if(CurrentChunk != null)
                CurrentChunk.RemoveObject(this);


        }

        public override void Render()
        {
            base.Render();
            // Disable hits when player is dead

            // Change the chunk and tell the server we've changed chunks
            UpdateChunk();
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
                Context.PropManager.RPC_DealDamage(prop.ChunkID, prop.GUID, hit.damageData.damageValue);

                if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                    Context.PropManager.Predict_DealDamage(prop.ChunkID, prop.GUID, hit.damageData.damageValue);
            }
        }

        void INetActor.ProjectileSpawnedCallback(Projectile projectile, ProjectileDefinition definition, ref FProjectileData data)
        {
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

            if (lastChunk == newChunk)
                return;

            CurrentChunk = newChunk;

            if (lastChunk != null)
                lastChunk.RemoveObject(this);

            if (newChunk != null)
                newChunk.AddObject(this);

            var oldChunks = new List<Chunk>(_cachedChunks);
            var newChunks = Context.ChunkManager.GetNearbyChunks(CurrentChunk.ChunkID, radius: 1);

            DiffChunks(oldChunks, newChunks, out var added, out var removed);

            _cachedChunks = newChunks;

            /*
            // Log removed chunks
            string removedLog = "Chunks Removed: " + removed.Count + "\n";
            foreach (var chunk in removed)
            {
                removedLog += $"  ChunkID: ({chunk.ChunkID.X}, {chunk.ChunkID.Y})\n";
            }

            // Log added chunks
            string addedLog = "Chunks Added: " + added.Count + "\n";
            foreach (var chunk in added)
            {
                addedLog += $"  ChunkID: ({chunk.ChunkID.X}, {chunk.ChunkID.Y})\n";
            }

            Debug.Log(addedLog + removedLog);
            */

            Context.ChunkManager.TryRemoveReplicatedChunks(removed);
            Context.ChunkManager.TryAddReplicatedChunks(added);
        }

        public void DiffChunks(List<Chunk> oldChunks, List<Chunk> newChunks,
                             out List<Chunk> addedChunks, out List<Chunk> removedChunks)
        {
            addedChunks = new List<Chunk>();
            removedChunks = new List<Chunk>();

            // Build HashSet of old IDs
            var oldIds = new HashSet<FChunkPosition>(oldChunks.Count);
            for (int i = 0; i < oldChunks.Count; i++)
            {
                oldIds.Add(oldChunks[i].ChunkID);
            }

            // Build HashSet of new IDs
            var newIds = new HashSet<FChunkPosition>(newChunks.Count);
            for (int i = 0; i < newChunks.Count; i++)
            {
                newIds.Add(newChunks[i].ChunkID);
            }

            // Added: newChunks that aren't in oldIds
            for (int i = 0; i < newChunks.Count; i++)
            {
                if (!oldIds.Contains(newChunks[i].ChunkID))
                {
                    addedChunks.Add(newChunks[i]);
                }
            }

            // Removed: oldChunks that aren't in newIds
            for (int i = 0; i < oldChunks.Count; i++)
            {
                if (!newIds.Contains(oldChunks[i].ChunkID))
                {
                    removedChunks.Add(oldChunks[i]);
                }
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_TakeProjectileHit(int projectileIndex, int damage)
        { 
            Health.ApplyDamage(damage);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_TakeHitNPC(int index, int damage)
        {
            Health.ApplyDamage(damage);
        }

        private string GetProjectName()
        {
            // Extract project name from Application.dataPath (e.g., "C:/Projects/MyGame_clone_0/Assets")
            string path = Application.dataPath;
            string projectName = Path.GetFileName(Path.GetDirectoryName(path));
            return string.IsNullOrEmpty(projectName) ? "DefaultInstance" : projectName;
        }

        public void MSG_DialogClosed()
        {
            Interactor.SetInteractType(EInteractType.None);
        }
    }
}