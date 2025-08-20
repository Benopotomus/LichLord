using DWD.Pooling;
using Fusion;
using LichLord.Buildables;
using LichLord.Projectiles;
using LichLord.Props;
using LichLord.World;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{

    public class NonPlayerCharacter : DWDObjectPoolObject, IHitTarget, IHitInstigator, INetActor, IChunkTrackable
    {
        private NonPlayerCharacterRuntimeState _runtimeState;
        public NonPlayerCharacterRuntimeState RuntimeState => _runtimeState;

        protected NonPlayerCharacterReplicator _replicator;
        public NonPlayerCharacterReplicator Replicator => _replicator;

        [SerializeField] private NonPlayerCharacterMovementComponent _movementComponent;
        public NonPlayerCharacterMovementComponent Movement => _movementComponent;

        [SerializeField] private NonPlayerCharacterStateComponent _stateComponent;
        public NonPlayerCharacterStateComponent State => _stateComponent;

        [SerializeField] private NonPlayerCharacterBrainComponent _brainComponent;
        public NonPlayerCharacterBrainComponent Brain => _brainComponent;

        [SerializeField] private NonPlayerCharacterHitReactComponent _hitReactComponent;
        public NonPlayerCharacterHitReactComponent HitReact => _hitReactComponent;

        [SerializeField] private NonPlayerCharacteHealthComponent _healthComponent;
        public NonPlayerCharacteHealthComponent Health => _healthComponent;

        [SerializeField] private NonPlayerCharacterWeaponsComponent _weaponsComponent;
        public NonPlayerCharacterWeaponsComponent Weapons => _weaponsComponent;

        [SerializeField] private NonPlayerCharacterAnimationController _animationController;
        public NonPlayerCharacterAnimationController AnimationController => _animationController;

        [SerializeField] private MuzzleComponent _muzzleComponent;
        public MuzzleComponent Muzzle => _muzzleComponent;

        [SerializeField] private Animator _animator;
        public Animator Animator => _animator;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        [SerializeField] private HurtboxComponent _hurtbox;
        public HurtboxComponent Hurtbox => _hurtbox;

        [SerializeField] private CapsuleCollider _collider;
        public CapsuleCollider Collider => _collider;

        private SceneContext _context;
        public SceneContext Context => _context;

        public INetActor NetActor => this;

        private FNetObjectID _netObjectID = new FNetObjectID();
        public FNetObjectID NetObjectID => _netObjectID;

        public float BonusRadius { get { return 1; } }

        private int _index;
        public int Index => _index;

        private ETeamID _teamId;
        public ETeamID TeamID => _teamId;

        // Cached list of Chunks for current and neighboring chunks
        private List<Chunk> _cachedChunks = new List<Chunk>();
        public IReadOnlyList<Chunk> CachedChunks => _cachedChunks.AsReadOnly();

        // IChunkTrackable
        private Chunk _currentChunk;
        public Chunk CurrentChunk { get => _currentChunk; set => _currentChunk = value; }
        public virtual Collider HurtBoxCollider { get { return Hurtbox.HurtBoxes[0]; } }

        public Vector3 Position => CachedTransform.position;
        public bool IsAttackable 
        { 
            get 
            {
                switch (_stateComponent.CurrentState)
                {
                    case ENPCState.Dead:
                    case ENPCState.Inactive:
                        return false;
                    default:
                        return true;
                }
            } 
        }

        [SerializeField]
        private GameObject redHat;

        [SerializeField]
        private GameObject blueHat;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacterReplicator replicator)
        {
            _context = replicator.Context;
            _replicator = replicator;
            _movementComponent.OnSpawned(ref spawnParams);
            _brainComponent.OnSpawned(ref spawnParams);
            _index = spawnParams.Index;
            UpdateChunk(_context.ChunkManager);

            _netObjectID.networkId = _replicator.Object.Id;
            _netObjectID.index = (byte)spawnParams.Index;
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState, 
            bool hasAuthority, 
            float renderDeltaTime, 
            float ping, 
            int tick)
        {
            _runtimeState = runtimeState;

            UpdateChunk(_context.ChunkManager);
            UpdateTeam(runtimeState);
            _stateComponent.UpdateStateChange(runtimeState, hasAuthority, tick);

            if (hasAuthority)
            {
                _movementComponent.AuthorityUpdate(runtimeState, renderDeltaTime, tick);
                _stateComponent.UpdateCurrentState(runtimeState, tick);
                _brainComponent.AuthorityUpdate(runtimeState, renderDeltaTime, tick);
            }
            else
            {
                _brainComponent.RemoteUpdate(runtimeState);
                _movementComponent.RemoteUpdate(runtimeState, renderDeltaTime, tick);
            }
        }

        private void UpdateTeam(NonPlayerCharacterRuntimeState runtimeState)
        {
            ETeamID newTeam = runtimeState.GetTeam();

            if (_teamId == newTeam)
                return;

            if (redHat != null && blueHat != null)
            {
                switch (newTeam)
                {
                    case ETeamID.EnemiesTeamA:
                        redHat.SetActive(false);
                        blueHat.SetActive(true);
                        break;
                    case ETeamID.EnemiesTeamB:
                        blueHat.SetActive(false);
                        redHat.SetActive(true);
                        break;
                }
            }

            _teamId = newTeam;
        }

        public void ProcessHit(ref FHitUtilityData hit) { }

        public void OnHitTaken(ref FHitUtilityData hit) { }

        void INetActor.ProjectileSpawnedCallback(Projectile projectile, ProjectileDefinition definition, ref FProjectileData data)
        {
            if (Replicator.HasStateAuthority)
                return;

            if (definition == null)
                return;

            if (!definition.ForcesRemoteAiming)
                return;

            if (projectile == null) 
                return;

            AnimationController.SetProjectileFrame(definition);
        }

        void IHitInstigator.HitPerformed(ref FHitUtilityData hit)
        {
            NetworkRunner runner = Context.Runner;
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

                if (!runner.IsSharedModeMasterClient)
                    npc.Replicator.Predict_DealDamageToNPC(npc.Index, hit.damageData.damageValue, hitReactIndex);
            }
            else if (hit.target is Prop prop)
            {
                Context.PropManager.RPC_DealDamage(prop.RuntimeState.chunk.ChunkID, prop.RuntimeState.index, hit.damageData.damageValue);

                if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                    Context.PropManager.Predict_DealDamage(prop.RuntimeState.chunk.ChunkID, prop.RuntimeState.index, hit.damageData.damageValue);
            }
            else if (hit.target is Stronghold stronghold)
            {
                stronghold.RPC_DealDamage(hit.damageData.damageValue);
            }
            else if (hit.target is PlayerCharacter pc)
            {
                if (pc.HasStateAuthority)
                {

                }
            }
            else if (hit.target is Buildable buildable)
            {
                buildable.Zone.RPC_DealDamage(buildable.RuntimeState.index, hit.damageData.damageValue);
            }
        }

        public void UpdateChunk(ChunkManager chunkManager)
        {
            var lastChunk = CurrentChunk;
            var newChunk = chunkManager.GetChunkAtPosition(CachedTransform.position);

            if (lastChunk != newChunk)
            {
                _currentChunk = newChunk;

                if(_currentChunk != null)
                    _cachedChunks = _context.ChunkManager.GetNearbyChunks(_currentChunk.ChunkID);

                if (lastChunk != null)
                    lastChunk.RemoveObject(this);

                if (newChunk != null)
                    newChunk.AddObject(this);
            }
        }

        public void StartRecycle()
        {
            Hurtbox.SetHitBoxesActive(false);
            Movement.AIFollower.rvoSettings.priority = 0.5f;
            Movement.SetFollowerUpdatePosition(false);
            Movement.SetFollowerUpdateRotation(false);
            Movement.SetFollowerLocalAvoidance(false);
            Movement.SetFollowerCanMove(false);
            _movementComponent.StartRecycle();
            DWDObjectPool.Instance.Recycle(this);
            UpdateChunk(Context.ChunkManager);
        }

        private NonPlayerCharacterDefinition _definition;
        public NonPlayerCharacterDefinition GetDefinition(ref FNonPlayerCharacterData data) 
        {
            if (data.DefinitionID == 0)
                return null;

            if (_definition == null || 
                _definition.TableID != data.DefinitionID)
            {
                _definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(data.DefinitionID);
            }

            return _definition;
        }
    }
}
