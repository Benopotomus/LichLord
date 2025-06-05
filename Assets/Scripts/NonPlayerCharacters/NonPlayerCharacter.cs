using DWD.Pooling;
using Pathfinding;
using Pathfinding.RVO;
using System.Data;
using UnityEngine;
using UnityEngine.AI;

namespace LichLord.NonPlayerCharacters
{
    // This is just the visual representation of an NPC
    // It updates while active. 
    // Because its always networked and updating, it should be processed
    // on the master client no matter what.
    
    public class NonPlayerCharacter : DWDObjectPoolObject, IHitTarget, IHitInstigator, INetActor
    {
        protected NonPlayerCharacterManager _manager;
        protected NonPlayerCharacterRuntimeState _runtimeState;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        [SerializeField] private FollowerEntity _follower;
        public FollowerEntity Agent => _follower;

        public HurtboxComponent Hurtbox;

        public INetActor NetActor => this;
        public FNetObjectID NetObjectID => new FNetObjectID();

        private Vector3 _lastPosition;
        [SerializeField] private Vector3 _moveTarget = Vector3.zero;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, NonPlayerCharacterManager manager)
        {
            _manager = manager;
            _runtimeState = runtimeState;
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            _follower.enabled = true;
            // Perform game logic updates

            if (Vector3.Distance(_transform.position, _moveTarget) < 5)
            {
                _moveTarget = new Vector3(
                   Random.Range(-30f, 30f),
                   0f, // Keep Y fixed
                   Random.Range(-30f, 30f)
               );
            }

            _follower.destination = _moveTarget;

            // Update the runtime state
            data.Position = _transform.position;
            data.Rotation = _transform.rotation;
            //data.Velocity = _transform.position - _lastPosition;
        }

        public void RemoteUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime, float ping)
        {
            _follower.enabled = false;
            _transform.position = Vector3.Lerp(_transform.position, data.Position, renderDeltaTime * 4f);
            _transform.rotation = Quaternion.Lerp(_transform.rotation, data.Rotation, renderDeltaTime * 10f);

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

        public void StartRecycle()
        {
            DWDObjectPool.Instance.Recycle(this);
        }

    }
}
