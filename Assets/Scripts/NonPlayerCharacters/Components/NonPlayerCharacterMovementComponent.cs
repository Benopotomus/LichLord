using Fusion;
using LichLord.Projectiles;
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

        [SerializeField] private Vector3 _worldVelocity;
        public Vector3 Velocity => _worldVelocity;

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

        private float _teleportDistanceSquared = 36;

        private float _10hrzSendDistance = 30.0f;
        private float _8hrzSendDistance = 60.0f;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            _lastPosition = runtimeState.GetPosition();
            _transform = transform;
        }

        public void AuthorityUpdate(NonPlayerCharacterRuntimeState runtimeState, float renderDeltaTime, int tick)
        {
            UpdateVelocity(renderDeltaTime);
            UpdateYawVelocity();
            _npc.AnimationController.UpdateAnimatonForMovement(runtimeState, _localVelocity, _yawVelocity, renderDeltaTime);
            TryWriteTransformData(runtimeState, tick);
        }

        public void RemoteUpdate(NonPlayerCharacterRuntimeState runtimeState, float renderDeltaTime, int tick)
        {
            if (runtimeState.GetState() == ENPCState.Dead || runtimeState.GetState() == ENPCState.Inactive)
                return;

            SetFollowerUpdatePosition(false);
            SetFollowerUpdateRotation(false);
            SetFollowerCanMove(false);
            SetFollowerLocalAvoidance(false);

            Vector3 statePosition = runtimeState.GetPosition();

            // If the data is too far away, teleport 
            if ((statePosition - NPC.CachedTransform.position).sqrMagnitude > _teleportDistanceSquared)
            {
                NPC.CachedTransform.position = statePosition;
            }
            else
            {
                // Smooth position with different lerp speeds for Y vs X/Z
                Vector3 currentPos = NPC.CachedTransform.position;
                Vector3 targetPos = statePosition;

                // Lerp X and Z with base speed, Y with faster speed
                float x = Mathf.Lerp(currentPos.x, targetPos.x, renderDeltaTime * 4f); // Base speed for X/Z
                float y = Mathf.Lerp(currentPos.y, targetPos.y, renderDeltaTime * 8f); // Faster speed for Y (2x base)
                float z = Mathf.Lerp(currentPos.z, targetPos.z, renderDeltaTime * 4f); // Base speed for X/Z

                NPC.CachedTransform.position = new Vector3(x, y, z);
            }

            // Smooth yaw only
            float currentYaw = NPC.CachedTransform.eulerAngles.y;
            float targetYaw = runtimeState.GetYaw();
            int targetPlayerIndex = runtimeState.GetTargetPlayerIndex();

            if (targetPlayerIndex > 0)
            {
                var targetPlayer = NPC.Context.NetworkGame.GetPlayerByIndex(targetPlayerIndex);
                if (targetPlayer != null)
                {
                    Vector3 dir = targetPlayer.Position - NPC.CachedTransform.position;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        // Calculate yaw toward the target player
                        float playerYaw = Quaternion.LookRotation(dir, Vector3.up).eulerAngles.y;

                        // Blend between facing player and existing projectile follow yaw
                        targetYaw = playerYaw;
                    }
                }
            }

            float lerpedYaw = Mathf.LerpAngle(currentYaw, targetYaw, renderDeltaTime * 10f);

            Vector3 currentEuler = NPC.CachedTransform.eulerAngles;
            NPC.CachedTransform.rotation = Quaternion.Euler(
                0,
                lerpedYaw,
                0
            );

            UpdateVelocity(renderDeltaTime);
            UpdateYawVelocity();
            _npc.AnimationController.UpdateAnimatonForMovement(runtimeState, _localVelocity, _yawVelocity, renderDeltaTime);
        }

        private void UpdateVelocity(float renderDeltaTime)
        {
            _worldVelocity = ((NPC.CachedTransform.position - _lastPosition) / renderDeltaTime);
            _lastPosition = NPC.CachedTransform.position;
            _localVelocity = NPC.CachedTransform.InverseTransformDirection(_worldVelocity);
        }

        private void UpdateYawVelocity()
        {
            Vector3 forward = _transform.forward;
            float currentYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _yawVelocity = currentYaw - _lastYaw;
            _lastYaw = currentYaw;
        }

        int _lastTick = -1;
        public void TryWriteTransformData(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            if(_lastTick == tick) 
                return;

            _lastTick = tick;

            int sendRateModulus = GetSendRateModulus();

            if ((tick + runtimeState.Index) % sendRateModulus != 0)
                return;

            WriteTransformData(runtimeState);
        }

        private int GetSendRateModulus()
        {
            var activePlayers = _npc.Context.NetworkGame.ActivePlayers;

            float minSqrDist = float.MaxValue;

            foreach (var player in activePlayers)
            {
                if (player.HasStateAuthority)
                    continue;

                float sqrDist = (player.CachedTransform.position - _npc.CachedTransform.position).sqrMagnitude;

                if (sqrDist < minSqrDist)
                    minSqrDist = sqrDist;
            }

            // Now decide based on nearest player distance
            if (minSqrDist < (_10hrzSendDistance * _10hrzSendDistance))
                return 3; // ~10.7 Hz
            if (minSqrDist < (_8hrzSendDistance * _8hrzSendDistance))
                return 4; // 8 Hz

            return 5; // ~6.4 Hz
        }

        private void WriteTransformData(NonPlayerCharacterRuntimeState runtimeState)
        {
            var data = runtimeState.Data;

            // Update the runtime state
            // Update position only if the change is significant
            const float POSITION_THRESHOLD = 0.15f;
            if (Mathf.Abs(NPC.CachedTransform.position.x - data.PositionX) > POSITION_THRESHOLD)
            {
                data.PositionX = NPC.CachedTransform.position.x;
            }

            if (Mathf.Abs(NPC.CachedTransform.position.y - data.PositionY) > (POSITION_THRESHOLD * 3))
            {
                data.PositionY = NPC.CachedTransform.position.y;
            }

            if (Mathf.Abs(NPC.CachedTransform.position.z - data.PositionZ) > POSITION_THRESHOLD)
            {
                data.PositionZ = NPC.CachedTransform.position.z;
            }

            if (NPC.Brain.AttackTarget is PlayerCharacter pc)
            {
                if (pc.SpawnComplete)
                {
                    if (data.RawCompressedYaw != (byte)(pc.PlayerIndex + 240))
                        data.RawCompressedYaw = (byte)(pc.PlayerIndex + 240);
                }
            }
            else
            {
                // Update rotation only if the change is significant
                const float ROTATION_THRESHOLD_DEGREES = 10.0f;
                float yawA = NPC.CachedTransform.eulerAngles.y;
                float yawB = data.Yaw;

                if (Mathf.Abs(Mathf.DeltaAngle(yawA, yawB)) > ROTATION_THRESHOLD_DEGREES)
                {
                    data.Yaw = yawA;
                }
            }

            runtimeState.CopyData(ref data);
            NPC.Replicator.ReplicateRuntimeState(runtimeState);
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

        public void OnStateAuthorityChanged(bool hasAuthority)
        {
            SetFollowerUpdatePosition(true);
            SetFollowerUpdateRotation(true);
            SetFollowerCanMove(true);
            SetFollowerLocalAvoidance(true);
        }
    }
}
