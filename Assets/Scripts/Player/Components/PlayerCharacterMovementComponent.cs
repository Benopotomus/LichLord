using UnityEngine;
using Fusion;

namespace LichLord
{
    public enum EMovementState : byte
    {
        None,
        Walking,
        Jumping,
        Flying
    }

    public class PlayerCharacterMovementComponent : ContextBehaviour
    {
        [SerializeField]
        private PlayerCharacter _pc;

        [SerializeField]
        private CharacterController _cc;
        public CharacterController CC => _cc;

        [Networked]
        private EMovementState _currentMoveState { get; set; }
        public EMovementState CurrentMoveState => _currentMoveState;
        private EMovementState _lastMoveState;

        [Header("References")]
        public Transform ScalingRoot;

        [Header("Movement Setup")]
        public float WalkSpeed = 4f;
        public float SprintSpeed = 7f;
        public float SprintAccelerationTime = 0.3f;
        public float UpGravity = -10f;
        public float DownGravity = -15f;
        public float TerminalVelocity = -15f;
        public float RotationSpeed = 8f;

        [Header("Movement Accelerations")]
        public float GroundAcceleration = 55f;
        public float GroundDeceleration = 25f;
        public float AirAcceleration = 25f;
        public float AirDeceleration = 1.3f;

        [Header("Flying Setup")]
        public float FlyAscendSpeed = 3f;
        public float FlyDescendSpeed = 6f;
        public float FlyAscendAcceleration = 4f;
        public float FlyDescendAcceleration = 2f;
        public float FlyVerticalBraking = 4f;
        public float FlyHorizontalSpeed = 7f;

        [Header("Jump Setup")]
        public float JumpImpulse = 5f;
        public float JumpHoldVelocity = 5.5f;
        public float JumpBufferTime = 0.2f;

        private float _jumpBufferTimer;
        private bool _jumpInputBuffered;
        private float _verticalInput;
        private float _castSpeedMultiplier = 1f;
        private float _currentMoveSpeed;

        private int _jumpCount { get; set; }

        public AudioSource FootstepSound;
        public AudioClip JumpAudioClip;
        public AudioClip LandAudioClip;

        [Header("VFX")]
        public ParticleSystem DustParticles;

        [Networked]
        private ref FWorldTransform _worldTransform => ref MakeRef<FWorldTransform>();
        public FWorldTransform WorldTransform => _worldTransform;

        private Vector3 _worldVelocity;
        public Vector3 WorldVelocity => _worldVelocity;

        private Vector3 _authorityMoveVelocity;
        private Vector3 _lastPosition;
        private Vector3 _localVelocity;
        private float _lastYaw;
        private float _yawVelocity;

        public void OnSpawned()
        {
            base.Spawned();

            if (HasStateAuthority)
                _currentMoveState = EMovementState.Walking;

            _jumpCount = 0;
            _localVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _jumpInputBuffered = false;
            _castSpeedMultiplier = 1f;
            _currentMoveSpeed = WalkSpeed;
        }

        public void OnDisable()
        {
            _lastMoveState = EMovementState.None;
        }

        public void OnRender(float renderDeltaTime)
        {
            UpdateVelocity(renderDeltaTime);
            UpdateYawVelocity();
            UpdateMovementState();

            _pc.AnimationController.UpdateAnimatonForMovement(_localVelocity, _yawVelocity, _currentMoveState, renderDeltaTime);
        }

        private void UpdateVelocity(float renderDeltaTime)
        {
            _worldVelocity = HasStateAuthority ? _authorityMoveVelocity : (_pc.CachedTransform.position - _lastPosition) / renderDeltaTime;
            _lastPosition = _pc.CachedTransform.position;
            _localVelocity = _pc.CachedTransform.InverseTransformDirection(_worldVelocity);
        }

        private void UpdateYawVelocity()
        {
            Vector3 forward = _pc.CachedTransform.forward;
            float currentYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _yawVelocity = currentYaw - _lastYaw;
            _lastYaw = currentYaw;
        }

        public void UpdateRemotePosition(float deltaTime)
        {
            if (HasStateAuthority)
                return;

            Vector3 lastPos = CC.transform.position;

            Vector3 newPosition = _worldTransform.Position;

            if ((newPosition - lastPos).sqrMagnitude > 36f)
            {
                CC.transform.position = newPosition;
            }
            else
            {
                float x = Mathf.Lerp(lastPos.x, _worldTransform.PositionX, deltaTime * 5f);
                float y = Mathf.Lerp(lastPos.y, _worldTransform.PositionY, deltaTime * 10f);
                float z = Mathf.Lerp(lastPos.z, _worldTransform.PositionZ, deltaTime * 5f);
                CC.transform.position = new Vector3(x, y, z);
            }

            CC.transform.rotation = Quaternion.Lerp(CC.transform.rotation, Quaternion.Euler(0f, _worldTransform.Yaw, 0f), deltaTime * 8f);
        }

        public void SetManeuverSpeedMultiplier(float multiplier)
        {
            _castSpeedMultiplier = multiplier;
        }

        public void SetLookRotation(ref FGameplayInput input)
        {
            _worldTransform.Yaw = input.LookRotation.y;
            _worldTransform.Pitch = Mathf.Clamp(input.LookRotation.x, -90, 90);
            CC.transform.rotation = Quaternion.Euler(0f, input.LookRotation.y, 0f);
        }

        public void ProcessInput(ref FGameplayInput input, float deltaTime)
        {
            if (!HasStateAuthority)
                return;

            SetLookRotation(ref input);
            ProcessMovement(ref input, deltaTime);
        }

        private void ProcessMovement(ref FGameplayInput input, float deltaTime)
        {
            if (!HasStateAuthority)
                return;

            bool isGrounded = CC.isGrounded;

            float gravity = DownGravity;

            var moveDirection = CC.transform.rotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);

            float targetSpeed = input.Sprint ? SprintSpeed : WalkSpeed;
            if (_currentMoveState == EMovementState.Flying)
                targetSpeed = FlyHorizontalSpeed;

            float lerpSpeed = Mathf.Lerp(_currentMoveSpeed, targetSpeed, deltaTime / SprintAccelerationTime);
            _currentMoveSpeed = lerpSpeed;
            float currentSpeed = _currentMoveSpeed * _castSpeedMultiplier;
            Vector3 desiredMoveVelocity = moveDirection * currentSpeed;

            float acceleration = desiredMoveVelocity == Vector3.zero
                ? (CC.isGrounded ? GroundDeceleration : AirDeceleration)
                : (CC.isGrounded ? GroundAcceleration : AirAcceleration);

            Vector3 currentHorizontalVelocity = new Vector3(_authorityMoveVelocity.x, 0f, _authorityMoveVelocity.z);
            Vector3 newHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, desiredMoveVelocity, acceleration * deltaTime);
            Vector3 moveVelocity = new Vector3(newHorizontalVelocity.x, _authorityMoveVelocity.y, newHorizontalVelocity.z);
            Vector3 horizontalVelocity = new Vector3(moveVelocity.x, 0f, moveVelocity.z);

            bool isRising = _authorityMoveVelocity.y > 0f;

            if (input.Jump && !input.JumpHeld)
            {
                _jumpInputBuffered = true;
                _jumpBufferTimer = JumpBufferTime;
            }

            if (_jumpInputBuffered)
            {
                _jumpBufferTimer -= deltaTime;
                if (_jumpBufferTimer <= 0f)
                {
                    _jumpInputBuffered = false;
                }
            }

            switch (_currentMoveState)
            {
                case EMovementState.Walking:
                    gravity = DownGravity;
                    _verticalInput = 0;
                    _jumpCount = 0;

                    if (isGrounded)
                    {
                        moveVelocity.y = (gravity ); // Ensure no vertical movement when grounded

                        if ((_jumpInputBuffered || input.Jump))
                        {
                            moveVelocity.y = JumpImpulse; // Apply jump here
                            _jumpCount = 1;
                            _currentMoveState = EMovementState.Jumping;
                            _jumpInputBuffered = false;
                            if (JumpAudioClip != null)
                                FootstepSound.PlayOneShot(JumpAudioClip);
                        }
                    }
                    else
                    {
                        moveVelocity.y += (gravity * deltaTime); // Reset vertical velocity to avoid abrupt fall
                        _jumpCount = 1;
                        _currentMoveState = EMovementState.Jumping;
                        _jumpInputBuffered = false;
                    }

                    break;

                case EMovementState.Jumping:
                    gravity = isRising ? UpGravity : DownGravity;

                    if ((_jumpInputBuffered || input.Jump) && _jumpCount < 2)
                    {
                        moveVelocity.y = JumpImpulse; // Apply double jump here
                        _jumpCount = 2;
                        _currentMoveState = EMovementState.Flying;
                        _verticalInput = FlyAscendSpeed;
                        _jumpInputBuffered = false;
                        gravity = 0;
                    }
                    else
                    {
                        moveVelocity.y += (gravity * deltaTime);
                    }

                    if (isGrounded)
                    {
                        _currentMoveState = EMovementState.Walking;
                        moveVelocity.y += (gravity * deltaTime);
                        _jumpCount = 0;
                        _jumpInputBuffered = false;
                        if (LandAudioClip != null)
                            FootstepSound.PlayOneShot(LandAudioClip);
                    }

                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, SprintSpeed * _castSpeedMultiplier);
                    moveVelocity = new Vector3(horizontalVelocity.x, moveVelocity.y, horizontalVelocity.z);
                    break;

                case EMovementState.Flying:
                    if (input.JumpHeld)
                    {
                        _verticalInput = Mathf.Lerp(_verticalInput, FlyAscendSpeed, FlyAscendAcceleration * deltaTime);
                    }
                    else if (input.CrouchHeld)
                    {
                        _verticalInput = Mathf.Lerp(_verticalInput, -FlyDescendSpeed, FlyDescendAcceleration * deltaTime);
                    }
                    else
                    {
                        _verticalInput = Mathf.Lerp(_verticalInput, 0, FlyVerticalBraking * deltaTime);
                    }

                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, FlyHorizontalSpeed * _castSpeedMultiplier);
                    moveVelocity = new Vector3(horizontalVelocity.x, _verticalInput, horizontalVelocity.z);

                    if (isGrounded)
                    {
                        _currentMoveState = EMovementState.Walking;
                        moveVelocity.y = 0f;
                        _jumpCount = 0;
                        _jumpInputBuffered = false;
                        if (LandAudioClip != null)
                        {
                            FootstepSound.PlayOneShot(LandAudioClip);
                        }
                    }

                    break;
            }

            _authorityMoveVelocity = moveVelocity; 
        }

        // Just for moving the controller
        bool _spawnComplete = false;
        public void FixedUpdate()
        {
            if(!HasStateAuthority)
                return;

            if (!_spawnComplete)
            {
                _spawnComplete = true;
                return;
            }

            CC.Move(_authorityMoveVelocity * Time.fixedDeltaTime);
        }

        public void OnFixedUpdateNetwork()
        {
            WriteData();
        }

        private void WriteData()
        {
            const float POSITION_THRESHOLD_XZ = 0.05f;
            if (Mathf.Abs(_pc.CachedTransform.position.x - _worldTransform.PositionX) > POSITION_THRESHOLD_XZ)
            {
                _worldTransform.PositionX = _pc.CachedTransform.position.x;
            }

            if (Mathf.Abs(_pc.CachedTransform.position.z - _worldTransform.PositionZ) > POSITION_THRESHOLD_XZ)
            {
                _worldTransform.PositionZ = _pc.CachedTransform.position.z;
            }

            const float POSITION_THRESHOLD_Y = 0.01f;
            if (Mathf.Abs(_pc.CachedTransform.position.y - _worldTransform.PositionY) > POSITION_THRESHOLD_Y)
            {
                _worldTransform.PositionY = _pc.CachedTransform.position.y;
            }

            _worldTransform.Yaw = _pc.CachedTransform.eulerAngles.y;
        }

        // Called from spawn parameters
        public void SetMovementState(EMovementState movementState)
        {
            _currentMoveState = movementState;
        }
        
        private void UpdateMovementState()
        {
            if (_currentMoveState == _lastMoveState)
                return;

            _pc.AnimationController.OnMovementStateChanged(_currentMoveState);

            _lastMoveState = _currentMoveState;
        }
    }
}