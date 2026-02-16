using UnityEngine;
using Fusion;
using UnityEngine.Rendering;
using LichLord.Projectiles;
using LichLord.NonPlayerCharacters;
using LichLord.World;
using LichLord.Props;
using System.Collections.Generic;
using System.IO;
using LichLord.Buildables;
using Example.ExpertMovement;

namespace LichLord
{
    public class PlayerCharacter : ThirdPersonExpertPlayer, IHitInstigator, IHitTarget, IChunkTrackable, IContextBehaviour
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
        public PlayerStatsComponent Stats;
        public CommanderComponent Commander;
        public PlayerWeaponsComponent Weapons;
        public PlayerInventoryComponent Inventory;
        public SummonerComponent Summoner;
        public PlayerCharacterIK IK;

        [SerializeField] private PlayerCharacterAnimationController _animationController;
        public PlayerCharacterAnimationController AnimationController => _animationController;

        public Renderer[] HeadRenderers;
        public GameObject[] FirstPersonOverlayObjects;

        [Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
        public string Nickname { get; set; }

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        public FNetObjectID NetObjectID 
        {
            get
            {
                FNetObjectID newId = new FNetObjectID();
                newId.SetObjectType(EObjectType.Player);
                newId.SetIndex(PlayerIndex);
                return newId;
            }
        }

        public float BonusRadius { get { return 0; } }

        //IHitInstigator
        public ETeamID TeamID => ETeamID.PlayerTeam;

        // IChunkTrackable
        private Chunk _chunk;
        public Chunk CurrentChunk { get { return _chunk; } set { _chunk = value; } }
        public Vector3 Position => CachedTransform.position;
        public Vector3 PredictedPosition => _cachedTransform.position + Movement.WorldVelocity;

        public bool IsAttackable { get { return true; } }
        public virtual Collider HurtBoxCollider { get { return Hurtbox.HurtBoxes[0]; } }

        // IHitTarget
        public IChunkTrackable ChunkTrackable => this;

        // Cached list of PropRuntimeState for current and neighboring chunks
        private List<PropRuntimeState> _cachedPropStates = new List<PropRuntimeState>();
        public IReadOnlyList<PropRuntimeState> CachedPropStates => _cachedPropStates.AsReadOnly();

        // Cached list of Chunks for current and neighboring chunks
        private List<Chunk> _cachedChunks = new List<Chunk>();
        public IReadOnlyList<Chunk> CachedChunks => _cachedChunks.AsReadOnly();

        [Networked]
        [SerializeField]
        public int PlayerIndex { get; set; }
        public SceneContext Context { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

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

        protected override void OnRenderUpdate()
        {
            base.OnRenderUpdate();
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

        void IHitInstigator.OnHitPerformed(ref FHitUtilityData hit)
        {
            if (hit.target is NonPlayerCharacter npc)
            {
                int currentAnimIndex = npc.State.CurrentAnimIndex;
                int currentAdditiveReactIndex = npc.HitReact.CurrentAdditiveReactIndex;

                // Normal hit react (0–3)
                int hitReactIndex = Random.Range(0, 4);
                if (hitReactIndex == currentAnimIndex)
                {
                    hitReactIndex = (currentAnimIndex + 1) % 4;
                }

                // Additive react — never 0 (only 1,2,3)
                int additiveReactIndex;

                // First try random in 1–3
                additiveReactIndex = Random.Range(1, 4);  // 1,2,3

                // If same as previous → pick next one (in 1–3 range)
                if (additiveReactIndex == currentAdditiveReactIndex)
                {
                    // Move to next, wrap around within 1–3
                    additiveReactIndex = currentAdditiveReactIndex + 1;
                    if (additiveReactIndex > 3)
                        additiveReactIndex = 1;
                }

                npc.Replicator.RPC_DealDamageToNPC(npc.LocalIndex, hit.damageData.damageValue, hitReactIndex, additiveReactIndex);

                if (!Runner.IsSharedModeMasterClient)
                    npc.Replicator.Predict_DealDamageToNPC(npc.LocalIndex, hit.damageData.damageValue, hitReactIndex, additiveReactIndex);
            }

            if (hit.target is Prop prop)
            {
                Context.PropManager.RPC_DealDamage(prop.ChunkID, prop.Index, hit.damageData.damageValue);

                if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                    Context.PropManager.Predict_DealDamage(prop.ChunkID, prop.Index, hit.damageData.damageValue);
            }
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
            {
                lastChunk.RemoveObject(this);
                lastChunk.RemoveHitTarget(this);
            }

            if (newChunk != null)
            {
                newChunk.AddObject(this);
                newChunk.AddHitTarget(this);
            }

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
            Stats.ApplyDamage(damage);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_TakeHitNPC(int index, int damage)
        {
            Stats.ApplyDamage(damage);
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