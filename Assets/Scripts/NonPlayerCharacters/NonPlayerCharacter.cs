using DWD.Pooling;
using LichLord.World;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    // This is just the visual representation of an NPC
    // It updates while active. 
    // Because its always networked and updating, it should be processed
    // on the master client no matter what.
    
    public class NonPlayerCharacter : DWDObjectPoolObject, IHitTarget, IHitInstigator, INetActor, IChunkTrackable
    {
        protected NonPlayerCharacterManager _manager;
        public NonPlayerCharacterManager Manager => _manager;

        protected NonPlayerCharacterReplicator _replicator;
        public NonPlayerCharacterReplicator Replicator => _replicator;

        [SerializeField] private NonPlayerCharacterMovementComponent _movementComponent;
        public NonPlayerCharacterMovementComponent Movement => _movementComponent;

        [SerializeField] private NonPlayerCharacterStateComponent _stateComponent;
        public NonPlayerCharacterStateComponent State => _stateComponent;

        [SerializeField] private NonPlayerCharacterBrainComponent _brainComponent;
        public NonPlayerCharacterBrainComponent Brain => _brainComponent;

        [SerializeField] private Animator _animator;
        public Animator Animator => _animator;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        [SerializeField] private HurtboxComponent _hurtbox;
        public HurtboxComponent Hurtbox => _hurtbox;

        public INetActor NetActor => this;
        public FNetObjectID NetObjectID => new FNetObjectID();

        private int _guid;
        public int GUID => _guid;

        private ETeamID _teamId;
        public ETeamID TeamID => _teamId;

        private Chunk _chunk;
        public Chunk CurrentChunk { get => _chunk; set => _chunk = value; }
        public Vector3 Position => CachedTransform.position;



        [SerializeField]
        private GameObject redHat;

        [SerializeField]
        private GameObject blueHat;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacterManager manager, NonPlayerCharacterReplicator replicator)
        {
            _manager = manager;
            _replicator = replicator;
            _movementComponent.OnSpawned(ref spawnParams);
            _stateComponent.OnSpawned(ref spawnParams);
            _brainComponent.OnSpawned(ref spawnParams);
            _teamId = spawnParams.teamId;
            _guid = spawnParams.index;
            UpdateChunk(Manager.Context.ChunkManager);

            switch (spawnParams.teamId)
            {
                case ETeamID.EnemiesTeamA:
                    redHat.SetActive(false);
                    break;
                case ETeamID.EnemiesTeamB:
                    blueHat.SetActive(false);
                    break;
            }
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            var definition = GetDefinition(ref data);
            if (definition == null)
                return;

            UpdateChunk(Manager.Context.ChunkManager);

            _stateComponent.UpdateState(ref data);

            _movementComponent.AuthorityUpdate(ref data, renderDeltaTime);

            _brainComponent.AuthorityUpdate(ref data, renderDeltaTime);

            _stateComponent.AuthorityUpdate(ref data, renderDeltaTime);
        }

        public void RemoteUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime, float ping)
        {
            UpdateChunk(Manager.Context.ChunkManager);

            var definition = GetDefinition(ref data);
            if (definition == null)
                return;

            _stateComponent.UpdateState(ref data);
            _movementComponent.RemoteUpdate(ref data, renderDeltaTime, ping);
        }

        public void OnFixedUpdate(ref FNonPlayerCharacterData data, int tick)
        {
            _movementComponent.OnFixedUpdate(ref data, tick);
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {

        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {

        }

        public void HitPerformed(ref FHitUtilityData hit)
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
            }
        }

        public void StartRecycle()
        {
            _movementComponent.StartRecycle();
            DWDObjectPool.Instance.Recycle(this);
            UpdateChunk(Manager.Context.ChunkManager);
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
