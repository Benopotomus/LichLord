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

        private Vector3 _lastPosition;
        private float _speedPercent;
        Vector3 _localVelocity;

        private int _animIDSpeedX = Animator.StringToHash("Velocity X");
        private int _animIDSpeedZ = Animator.StringToHash("Velocity Z");
        private int _animIDMoving = Animator.StringToHash("Moving");

        bool _followerEnabled = true;
        bool _followerUpdateRotation = true;
        bool _followerLocalAvoidance = true;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
            _lastPosition = spawnParams.position;
            _follower.updatePosition = true;
            _follower.updateRotation = true;
            _follower.destination = NPC.Brain.MoveTarget;
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            switch (NPC.State.CurrentState)
            {
                case ENonPlayerState.Maneuver_1:
                case ENonPlayerState.Maneuver_2:
                case ENonPlayerState.Maneuver_3:
                case ENonPlayerState.Maneuver_4:
                    SetFollowerEnabled(false);
                    SetFollowLocalAvoidance(true);
                    break;
                case ENonPlayerState.Inactive:
                case ENonPlayerState.HitReact:
                case ENonPlayerState.Dead:
                    SetFollowerEnabled(false);
                    SetFollowLocalAvoidance(false);
                    return;
                default:
                    SetFollowerEnabled(true);
                    SetFollowLocalAvoidance(true);
                    break;
            }

            _speedPercent = _follower.velocity.magnitude / _npc.GetDefinition(ref data).WalkSpeed;
            
            UpdateVelocity(ref data, renderDeltaTime);
            UpdateAnimator(ref data, renderDeltaTime);
        }

        public void RemoteUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime, float ping)
        {
            SetFollowerEnabled(false);

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
            UpdateAnimator(ref data, renderDeltaTime);
        }

        private void UpdateVelocity(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            _velocity = ((NPC.CachedTransform.position - _lastPosition) / renderDeltaTime);
            _lastPosition = NPC.CachedTransform.position;
            _localVelocity = NPC.CachedTransform.InverseTransformDirection(_velocity);
        }

        private void UpdateAnimator(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            bool isMoving = _velocity.magnitude > 0.1f;

            if (NPC.State.CurrentState != ENonPlayerState.Idle)
                isMoving = false;

            Vector3 animationVelocity = _localVelocity / NPC.GetDefinition(ref data).WalkSpeed;
            NPC.Animator.SetBool(_animIDMoving, isMoving);
            NPC.Animator.SetFloat(_animIDSpeedX, animationVelocity.x, 0.1f, renderDeltaTime);
            NPC.Animator.SetFloat(_animIDSpeedZ, animationVelocity.z, 0.1f, renderDeltaTime);
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

            //NPC.Replicator.UpdateNPCData(data);
        }

        public void SetFollowerEnabled(bool newEnabled)
        {
            if (newEnabled == _followerEnabled)
                return;

            _follower.updatePosition = newEnabled;
            _follower.updateRotation = newEnabled;
            _follower.canMove = newEnabled;

            _followerEnabled = newEnabled;
        }

        public void SetFollowUpdateRotation(bool newEnabled)
        {
            if(_followerUpdateRotation == newEnabled) 
                return;

            _follower.updateRotation = newEnabled;

            _followerUpdateRotation = newEnabled;
        }

        public void SetFollowLocalAvoidance(bool newEnabled)
        {
            if (_followerLocalAvoidance == newEnabled)
                return;

            _follower.enableLocalAvoidance = newEnabled;

            _followerLocalAvoidance = newEnabled;
        }

        public void StartRecycle()
        {
            _follower.enableLocalAvoidance = false;
            _follower.updatePosition = false;
            _follower.updateRotation = false;
            _follower.canMove = false;
            NPC.Movement.AIFollower.updateRotation = false;
            NPC.Movement.AIFollower.updatePosition = false;
        }
    }
}
