using Pathfinding;
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

        [SerializeField] private LayerMask _layerMask;

        private Vector3 _lastPosition;
        private Vector3 _localVelocity;
        private float _lastYaw;
        private float _yawVelocity;

        bool _followerUpdatePosition = true;
        bool _followerUpdateRotation = true;
        bool _followerLocalAvoidance = true;
        bool _followerCanMove = true;
        float _followerMaxSpeed = 5f;

        private Transform _transform;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
            _lastPosition = spawnParams.position;
            _transform = transform;
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            UpdateVelocity(ref data, renderDeltaTime);
            UpdateYawVelocity();
            _npc.AnimationController.UpdateAnimatonForMovement(ref data, _localVelocity, _yawVelocity, renderDeltaTime);
        }

        public void RemoteUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime, float ping)
        {
            SetFollowerUpdatePosition(false);
            SetFollowerCanMove(false);
            SetFollowerLocalAvoidance(false);

            // Smooth position
            NPC.CachedTransform.position = Vector3.Lerp(
                NPC.CachedTransform.position,
                data.Position,
                renderDeltaTime * 4f
            );

            // Smooth yaw only
            float currentYaw = NPC.CachedTransform.eulerAngles.y;
            float targetYaw = data.Yaw; // Assume this is in degrees

            float lerpedYaw = Mathf.LerpAngle(currentYaw, targetYaw, renderDeltaTime * 10f);

            // Keep existing pitch and roll
            Vector3 currentEuler = NPC.CachedTransform.eulerAngles;
            NPC.CachedTransform.rotation = Quaternion.Euler(
                0,
                lerpedYaw,
                0
            );

            UpdateVelocity(ref data, renderDeltaTime);
            UpdateYawVelocity();
            _npc.AnimationController.UpdateAnimatonForMovement(ref data, _localVelocity, _yawVelocity, renderDeltaTime);
        }

        private void UpdateVelocity(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            _velocity = ((NPC.CachedTransform.position - _lastPosition) / renderDeltaTime);
            _lastPosition = NPC.CachedTransform.position;
            _localVelocity = NPC.CachedTransform.InverseTransformDirection(_velocity);
        }

        private void UpdateYawVelocity()
        {
            Vector3 forward = _transform.forward;
            float currentYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _yawVelocity = currentYaw - _lastYaw;
            _lastYaw = currentYaw;
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
            const float ROTATION_THRESHOLD_DEGREES = 5.0f;
            float yawA = NPC.CachedTransform.eulerAngles.y;
            float yawB = data.Yaw;

            if (Mathf.Abs(Mathf.DeltaAngle(yawA, yawB)) > ROTATION_THRESHOLD_DEGREES)
            {
                data.Yaw = yawA;
            }
        }

        public void SetFollowerUpdatePosition(bool newEnabled)
        {
            if (newEnabled == _followerUpdatePosition)
                return;

            _follower.updatePosition = newEnabled;

            _followerUpdatePosition = newEnabled;
        }

        public void SetFollowerUpdateRotation(bool newEnabled)
        {
            if (_followerUpdateRotation == newEnabled)
                return;

            _follower.updateRotation = newEnabled;

            _followerUpdateRotation = newEnabled;
        }

        public void SetFollowerCanMove(bool newCanMove)
        {
            if (newCanMove == _followerCanMove)
                return;

            _follower.canMove = newCanMove;

            _followerCanMove = newCanMove;
        }

        public void SetFollowerLocalAvoidance(bool newEnabled)
        {
            if (_followerLocalAvoidance == newEnabled)
                return;

            _follower.enableLocalAvoidance = newEnabled;

            _followerLocalAvoidance = newEnabled;
        }

        public void SetFollowerMaxSpeed(float newSpeed)
        {
            if (_followerMaxSpeed == newSpeed)
                return;

            _follower.maxSpeed = newSpeed;

            _followerMaxSpeed = newSpeed;
        }

        public void StartRecycle()
        {
        }
    }
}
