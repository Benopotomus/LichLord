using DWD.Pooling;
using Fusion;
using LichLord.Projectiles;
using LichLord.Props;
using LichLord.World;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{

    public class NonPlayerCharacter : DWDObjectPoolObject, IHitTarget, IHitInstigator, INetActor, IChunkTrackable
    {
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

        private int _guid;
        public int GUID => _guid;

        private ETeamID _teamId;
        public ETeamID TeamID => _teamId;

        // Cached list of Chunks for current and neighboring chunks
        private List<Chunk> _cachedChunks = new List<Chunk>();
        public IReadOnlyList<Chunk> CachedChunks => _cachedChunks.AsReadOnly();

        // IChunkTrackable
        private Chunk _currentChunk;
        public Chunk CurrentChunk { get => _currentChunk; set => _currentChunk = value; }

        public Vector3 Position => CachedTransform.position;
        public bool IsAttackable 
        { 
            get 
            {
                switch (_stateComponent.CurrentState)
                {
                    case ENonPlayerState.Dead:
                    case ENonPlayerState.Inactive:
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
            _stateComponent.OnSpawned(ref spawnParams);
            _brainComponent.OnSpawned(ref spawnParams);
            _guid = spawnParams.index;
            UpdateChunk(_context.ChunkManager);

            _netObjectID.networkId = _replicator.Object.Id;
            _netObjectID.index = (ushort)spawnParams.index;
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, 
            float renderDeltaTime, 
            int tick)
        {
            var definition = GetDefinition(ref data);
            if (definition == null)
                return;

            UpdateChunk(_context.ChunkManager);
            UpdateTeam(ref data);

            _stateComponent.UpdateState(ref data, true);

            _movementComponent.AuthorityUpdate(ref data, renderDeltaTime);

            _stateComponent.AuthorityUpdate(ref data, renderDeltaTime);

            _brainComponent.AuthorityUpdate(ref data, renderDeltaTime, tick);
        }

        public void RemoteUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime, float ping)
        {
            UpdateChunk(_context.ChunkManager);
            UpdateTeam(ref data);

            var definition = GetDefinition(ref data);
            if (definition == null)
                return;

            _stateComponent.UpdateState(ref data, false);

            switch (_stateComponent.CurrentState)
            {
                case ENonPlayerState.Dead:
                case ENonPlayerState.Inactive:
                    break;
                default:
                    _movementComponent.RemoteUpdate(ref data, renderDeltaTime, ping);
                    break;
            }
        }

        private void UpdateTeam(ref FNonPlayerCharacterData data)
        {
            ETeamID newTeam = data.Team;

            if (_teamId == newTeam)
                return;

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

            _teamId = newTeam;
        }

        public void OnFixedUpdate(ref FNonPlayerCharacterData data, int tick)
        {
            _movementComponent.OnFixedUpdate(ref data, tick);

            _brainComponent.OnFixedUpdate(ref data, tick);
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {

        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {

        }

        void INetActor.ProjectileSpawnedCallback(Projectile projectile, ProjectileDefinition definition, ref FProjectileData data)
        {
            if (projectile == null) 
                return;

            if (definition == null)
                return;

            if (!definition.ForcesRemoteAiming)
                return;

            if (Replicator.HasStateAuthority)
                return;

            Movement.SetProjectileYaw(ref data);
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

                npc.Replicator.RPC_DealDamageToNPC(npc.GUID, hit.damageData.damageValue, hitReactIndex);

                if (!runner.IsSharedModeMasterClient)
                    npc.Replicator.Predict_DealDamageToNPC(npc.GUID, hit.damageData.damageValue, hitReactIndex);
            }

            if (hit.target is Prop prop)
            {
                Context.PropManager.RPC_DealDamageToProp(prop.RuntimeState.guid, hit.damageData.damageValue);

                if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                    Context.PropManager.Predict_DealDamageToProp(prop.RuntimeState.guid, hit.damageData.damageValue);
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
