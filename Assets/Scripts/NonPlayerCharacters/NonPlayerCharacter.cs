using DWD.Pooling;
using Fusion;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    // This is just the visual representation of an NPC
    // It updates while active. 
    // Because its always networked and updating, it should be processed
    // on the master client no matter what.
    
    public class NonPlayerCharacter : DWDObjectPoolObject, IHitTarget, IHitInstigator, INetActor
    {
        protected NonPlayerCharacterManager _manager;
        public NonPlayerCharacterManager Manager => _manager;

        protected NonPlayerCharacterReplicator _replicator;
        public NonPlayerCharacterReplicator Replicator => _replicator;

        [SerializeField] private NonPlayerCharacterMovementComponent _movementComponent;
        public NonPlayerCharacterMovementComponent Movement => _movementComponent;

        [SerializeField] private Animator _animator;
        public Animator Animator => _animator;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        public HurtboxComponent Hurtbox;

        public INetActor NetActor => this;
        private FNetObjectID _netObjectId = new FNetObjectID();
        public FNetObjectID NetObjectID => _netObjectId;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacterManager manager, NonPlayerCharacterReplicator replicator)
        {
            _manager = manager;
            _replicator = replicator;
            _netObjectId.networkId = _manager.Object.Id;
            _netObjectId.index = (byte)spawnParams.index;
            _movementComponent.OnSpawned(ref spawnParams);
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            var definition = GetDefinition(ref data);
            if (definition == null)
                return;

            _movementComponent.AuthorityUpdate(ref data, renderDeltaTime);
        }

        public void RemoteUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime, float ping)
        {
            var definition = GetDefinition(ref data);
            if (definition == null)
                return;

            _movementComponent.RemoteUpdate(ref data, renderDeltaTime, ping);
        }

        public void OnFixedUpdate(ref FNonPlayerCharacterData data, int tick)
        {
            _movementComponent.OnFixedUpdate(ref data, tick);
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {

        }

        public void ApplyDamage(Vector3 impulse, int damage)
        {
            Debug.Log("Apply Damage: " + damage + ", index: " + _netObjectId.index);

            if (_replicator.TryGetNPCData(_netObjectId.index, out FNonPlayerCharacterData data))
            {
                data.State = ENonPlayerState.Inactive;
                data.Health = Mathf.Clamp(data.Health - 10, 0, 1000);
                _replicator.UpdateNPCData(data);
            }
            StartRecycle();
        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {

        }

        public void HitPerformed(ref FHitUtilityData hit)
        {

        }

        public void StartRecycle()
        {
            _movementComponent.StartRecycle();
            DWDObjectPool.Instance.Recycle(this);
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
