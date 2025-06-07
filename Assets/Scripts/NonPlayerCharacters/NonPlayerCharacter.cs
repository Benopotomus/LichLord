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
            if (_manager == null)
            {
                Debug.LogWarning($"[NonPlayerCharacter] Cannot process hit: NPCManager is null for guid {_netObjectId.index}.");
                return;
            }

            NetworkRunner runner = _manager.Runner;

            Debug.Log($"[NPC] Processing hit for guid {_netObjectId.index} with damage 9001");

            foreach (PlayerRef player in runner.ActivePlayers)
            {
                NetworkObject playerObj = runner.GetPlayerObject(player);
                if (playerObj != null)
                {
                    /// How do i just send this to the masterclient player?
                    RelayPlayer relayPlayer = playerObj.GetComponent<RelayPlayer>();

                    relayPlayer.RaiseEvent(new NonPlayerCharacterDamageEvent
                    {
                        guid = _netObjectId.index,
                        impulse = Vector3.zero,
                        damage = 9001
                    });
                }
            }
        }

        public void ApplyDamage(Vector3 impulse, int damage)
        {
            Debug.Log("Apply Damage " + damage); 
        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {

        }

        public void HitPerformed(ref FHitUtilityData hit)
        {

        }

        public void StartRecycle()
        {
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
