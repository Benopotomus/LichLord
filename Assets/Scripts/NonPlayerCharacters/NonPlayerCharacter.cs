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
        public FollowerEntity AIFollower => _follower;

        public HurtboxComponent Hurtbox;

        public INetActor NetActor => this;
        public FNetObjectID NetObjectID => new FNetObjectID();

        [SerializeField] private Vector3 _moveTarget = Vector3.zero;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, NonPlayerCharacterManager manager)
        {
            _manager = manager;
            _runtimeState = runtimeState;

        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            // Perform game logic updates

            if (Vector3.Distance(_transform.position, _moveTarget) < 3)
            {
                _moveTarget = new Vector3(
                   Random.Range(-10f, 10f),
                   0f, // Keep Y fixed
                   Random.Range(-10f, 10f)
               );
            }

            _follower.canMove = true;
            _follower.destination = _moveTarget;
            _follower.SearchPath();
            // Update the runtime state
            data.Position = _transform.position;
            data.Rotation = _transform.rotation;
        }


        public void RemoteUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime, float ping)
        {
            _follower.canMove = false;

            _follower.position = Vector3.Lerp(_transform.position, data.Position, renderDeltaTime * 4f);
            _follower.rotation = Quaternion.Lerp(_transform.rotation, data.Rotation, renderDeltaTime * 10f);
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
