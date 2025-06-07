using Pathfinding;
using TMPro;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterMovementComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField] private FollowerEntity _follower;
        public FollowerEntity AIFollower => _follower;

        [SerializeField] private Vector3 _velocity;
        public Vector3 Velocity => _velocity;

        [SerializeField] private bool _isGrounded;
        public bool IsGrounded => _isGrounded;

        [SerializeField] private Vector3 _moveTarget = Vector3.zero;

        private Vector3 _lastPosition;
        private float _speedPercent;

        private int _animIDSpeedX = Animator.StringToHash("Velocity X");
        private int _animIDSpeedZ = Animator.StringToHash("Velocity Z");
        private int _animIDMoving = Animator.StringToHash("Moving");

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
            _lastPosition = spawnParams.position;
            _follower.updatePosition = true;
            _follower.updateRotation = true;
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            var definition = _npc.GetDefinition(ref data);
            _speedPercent = _follower.velocity.magnitude / definition.WalkSpeed;

            UpdateVelocity(ref data, renderDeltaTime);
  
            if (Vector3.Distance(NPC.CachedTransform.position, _moveTarget) < 3)
            {
                _moveTarget = new Vector3(
                   Random.Range(-50f, 50f),
                   0f, // Keep Y fixed
                   Random.Range(-50f, 50f)
               );
            }

            _follower.canMove = true;
            _follower.destination = _moveTarget;
        }

        public void RemoteUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime, float ping)
        {
            _follower.canMove = false;

            _follower.position = Vector3.Lerp(NPC.CachedTransform.position, data.Position, renderDeltaTime * 4f);
            _follower.rotation = Quaternion.Lerp(NPC.CachedTransform.rotation, data.Rotation, renderDeltaTime * 10f);

            _speedPercent = NonPlayerCharacterDataUtility.GetCurrentSpeedPercent(data);
            UpdateVelocity(ref data, renderDeltaTime);
        }

        private void UpdateVelocity(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            _velocity = NPC.CachedTransform.position - _lastPosition;
            _lastPosition = NPC.CachedTransform.position;

            NPC.Animator.SetBool(_animIDMoving, _speedPercent > 0.01f);
            Vector3 normalizedVelocity = (_velocity.normalized * _speedPercent);// * _npc.GetDefinition(ref data).WalkSpeed) ;

            Vector3 localVelocity = NPC.CachedTransform.InverseTransformDirection(normalizedVelocity);
            NPC.Animator.SetFloat(_animIDSpeedX, localVelocity.x, 0.1f, renderDeltaTime);
            NPC.Animator.SetFloat(_animIDSpeedZ, localVelocity.z, 0.1f, renderDeltaTime);
        }

        public void OnFixedUpdate(ref FNonPlayerCharacterData data, int tick)
        {
            //30 server ticks/second
            //10 send ticks/second
            if (tick % 3 != 0)
                return;

            WriteData(ref data);
        }

        private void WriteData(ref FNonPlayerCharacterData data)
        {

            // Update the runtime state
            // Update position only if the change is significant
            const float POSITION_THRESHOLD = 0.1f;
            if (Mathf.Abs(NPC.CachedTransform.position.x - data.PositionX) > POSITION_THRESHOLD)
            {
                data.PositionX = NPC.CachedTransform.position.x;
            }

            if (Mathf.Abs(NPC.CachedTransform.position.y - data.PositionY) > POSITION_THRESHOLD)
            {
                data.PositionY = NPC.CachedTransform.position.y;
            }

            if (Mathf.Abs(NPC.CachedTransform.position.z - data.PositionZ) > POSITION_THRESHOLD)
            {
                data.PositionZ = NPC.CachedTransform.position.z;
            }

            // Update rotation only if the change is significant
            const float ROTATION_THRESHOLD_DEGREES = 5.0f; // 1 degree
            if (Quaternion.Angle(NPC.CachedTransform.rotation, data.Rotation) > ROTATION_THRESHOLD_DEGREES)
            {
                data.Rotation = NPC.CachedTransform.rotation;
            }

            NonPlayerCharacterDataUtility.SetCurrentSpeedPercent(_speedPercent, ref data);

            NPC.Replicator.UpdateNPCData(data);
        }

        public void StartRecycle()
        {
            _follower.enableLocalAvoidance = false;
            _follower.updatePosition = false;
            _follower.updateRotation = false;
            _follower.canMove = false;
        }
    }
}
