using UnityEngine;
using Fusion;
using LichLord.Props;

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
        public float GroundAcceleration = 10f;
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
        private int _jumpCount { get; set; }

        public AudioSource FootstepSound;
        public AudioClip JumpAudioClip;
        public AudioClip LandAudioClip;

        [Header("VFX")]
        public ParticleSystem DustParticles;
        [SerializeField]
        private VisualEffectBase _flyVisualEffect;

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

        public Transform LookTarget { get; set; }

        public void UpdateLookRotation(float deltaTime, float lerpSpeed)
        {
            Quaternion targetRotation = LookTarget != null
                ? GetLookTargetRotation()
                : Context.Camera._cameraFollowTarget.rotation;

            // Extract desired yaw and pitch from target rotation
            float rawPitch = targetRotation.eulerAngles.x;
            float yaw = targetRotation.eulerAngles.y;
            float normalizedPitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;

            // Lerp only the Yaw (Y-axis) for horizontal character rotation
            Quaternion currentRotation = CC.transform.rotation;
            Quaternion targetYawRotation = Quaternion.Euler(0f, yaw, 0f);
            Quaternion lerpedRotation = Quaternion.Slerp(currentRotation, targetYawRotation, lerpSpeed * deltaTime);

            CC.transform.rotation = lerpedRotation;

            // Update world transform with latest values
            _worldTransform.Yaw = lerpedRotation.eulerAngles.y;
            _worldTransform.Pitch = normalizedPitch;
        }

        // Get desired rotation when facing interactable
        public Quaternion GetLookTargetRotation()
        {
            Vector3 faceDirection = (LookTarget.position - CC.transform.position).normalized;
            return Quaternion.LookRotation(faceDirection);
        }

        public void ProcessInput(ref FGameplayInput input, float deltaTime)
        {
            if (!HasStateAuthority)
                return;

            ProcessMovement(ref input, deltaTime);
        }

        public void ProcessMovement(ref FGameplayInput input, float deltaTime)
        {
            if (!HasStateAuthority)
                return;

            bool isGrounded = IsGrounded();

            Vector3 lastHorizontalVelocity = new Vector3(_authorityMoveVelocity.x, 0f, _authorityMoveVelocity.z);
            float gravity = DownGravity;

            var inputDirection = CC.transform.rotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);

            float targetSpeed = input.Sprint ? SprintSpeed : WalkSpeed;
            if (_currentMoveState == EMovementState.Flying)
                targetSpeed = FlyHorizontalSpeed;

            // Adjust speed based on movement direction
            if (inputDirection != Vector3.zero)
            {
                Vector3 forward = CC.transform.forward;
                float dot = Vector3.Dot(inputDirection.normalized, forward);
                float t = (dot + 1.0f) / 2.0f; // Normalize dot from [-1, 1] to [0, 1]
                float speedMultiplier = Mathf.SmoothStep(0.75f, 1.0f, t);
                targetSpeed *= speedMultiplier;
            }

            // Adjust speed by maneuver
            targetSpeed *= _pc.Maneuvers.GetMoveSpeedMultiplier();

            Vector3 desiredMoveVelocity = inputDirection * targetSpeed;

            float acceleration = desiredMoveVelocity == Vector3.zero
                ? (isGrounded ? GroundDeceleration : AirDeceleration)
                : (isGrounded ? GroundAcceleration : AirAcceleration);

            Vector3 newHorizontalVelocity = Vector3.Lerp(lastHorizontalVelocity, desiredMoveVelocity, acceleration * deltaTime);
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
                    _jumpCount = 0;

                    if (isGrounded)
                    {
                        // Apply the grounded gravity value
                        moveVelocity.y = gravity;

                        if ((_jumpInputBuffered || input.Jump))
                        {
                            moveVelocity.y = JumpImpulse; // Apply jump here
                            _jumpCount = 1;
                            _currentMoveState = EMovementState.Jumping;
                            _jumpInputBuffered = false;
                            _jumpInitiated = true;
                            if (JumpAudioClip != null)
                                FootstepSound.PlayOneShot(JumpAudioClip);
                        }
                    }
                    else
                    {
                        // Apply Gravity
                        moveVelocity.y += (gravity * deltaTime);
                        moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);

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

                        _jumpInputBuffered = false;
                        gravity = 0;
                    }
                    else
                    {
                        // Apply Gravity
                        moveVelocity.y += (gravity * deltaTime);
                        moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);
                    }

                    if (isGrounded)
                    {
                        // Apply Gravity
                        moveVelocity.y += (gravity * deltaTime);
                        moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);

                        _currentMoveState = EMovementState.Walking;

                        _jumpCount = 0;
                        _jumpInputBuffered = false;
                        if (LandAudioClip != null)
                            FootstepSound.PlayOneShot(LandAudioClip);
                    }

                    moveVelocity = new Vector3(horizontalVelocity.x, moveVelocity.y, horizontalVelocity.z);
                    break;

                case EMovementState.Flying:
                    if (input.JumpHeld)
                    {
                        moveVelocity.y = Mathf.Lerp(moveVelocity.y, FlyAscendSpeed, FlyAscendAcceleration * deltaTime);
                    }
                    else if (input.CrouchHeld)
                    {
                        moveVelocity.y = Mathf.Lerp(moveVelocity.y, -FlyDescendSpeed, FlyDescendAcceleration * deltaTime);
                    }
                    else
                    {
                        moveVelocity.y = Mathf.Lerp(moveVelocity.y, 0, FlyVerticalBraking * deltaTime);
                    }

                    moveVelocity = new Vector3(horizontalVelocity.x, moveVelocity.y, horizontalVelocity.z);

                    if (isGrounded)
                    {
                        // Apply the grounded gravity value
                        moveVelocity.y = gravity;
                        _currentMoveState = EMovementState.Walking;
                    }

                    break;
            }

            _authorityMoveVelocity = moveVelocity;
        }
        
        public void ProcessInteractMovement(Vector3 interactablePosition, float deltaTime)
        {
            // Lerp and brake towards the interactable

            if (!HasStateAuthority)
                return;

            bool isGrounded = CC.isGrounded;

            float gravity = DownGravity;

            float targetSpeed = 0;

            var moveDirection = (interactablePosition - CC.transform.position).normalized;

            Vector3 lastHorizontalVelocity = new Vector3(_authorityMoveVelocity.x, 0f, _authorityMoveVelocity.z);

            float currentSpeed = Mathf.Lerp(lastHorizontalVelocity.magnitude, targetSpeed, 20f * deltaTime);

            Vector3 desiredMoveVelocity = moveDirection * currentSpeed;

            float acceleration = desiredMoveVelocity == Vector3.zero
                ? (isGrounded ? GroundDeceleration : AirDeceleration)
                : (isGrounded ? GroundAcceleration : AirAcceleration);


            Vector3 newHorizontalVelocity = Vector3.Lerp(lastHorizontalVelocity, desiredMoveVelocity, acceleration * deltaTime);
            Vector3 moveVelocity = new Vector3(newHorizontalVelocity.x, _authorityMoveVelocity.y, newHorizontalVelocity.z);
            Vector3 horizontalVelocity = new Vector3(moveVelocity.x, 0f, moveVelocity.z);

            bool isRising = _authorityMoveVelocity.y > 0f;

            switch (_currentMoveState)
            {
                case EMovementState.Walking:
                    gravity = DownGravity;

                    if (isGrounded)
                    {
                        // Apply the grounded gravity value
                        moveVelocity.y = gravity;
                    }
                    else
                    {
                        // Apply Gravity
                        moveVelocity.y += (gravity * deltaTime);
                        moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);

                        _jumpCount = 1;
                        _currentMoveState = EMovementState.Jumping;
                        _jumpInputBuffered = false;
                    }

                    break;

                case EMovementState.Jumping:
                    gravity = isRising ? UpGravity : DownGravity;

                    // Apply Gravity
                    moveVelocity.y += (gravity * deltaTime);
                    moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);
                 
                    if (isGrounded)
                    {
                        // Apply Gravity
                        moveVelocity.y += (gravity * deltaTime);
                        moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);

                        _currentMoveState = EMovementState.Walking;

                        _jumpCount = 0;
                        _jumpInputBuffered = false;
                        if (LandAudioClip != null)
                            FootstepSound.PlayOneShot(LandAudioClip);
                    }

                    moveVelocity = new Vector3(horizontalVelocity.x, moveVelocity.y, horizontalVelocity.z);
                    break;

                case EMovementState.Flying:

                    moveVelocity.y = Mathf.Lerp(moveVelocity.y, 0, 20f * deltaTime);        
                    moveVelocity = new Vector3(horizontalVelocity.x, moveVelocity.y, horizontalVelocity.z);

                    if (isGrounded)
                    {
                        // Apply the grounded gravity value
                        moveVelocity.y = gravity;
                        _currentMoveState = EMovementState.Walking;
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

            _jumpInitiated = false;
            CC.Move(_authorityMoveVelocity * Time.fixedDeltaTime);
        }

        public void WritePosition()
        {
            const float POSITION_THRESHOLD_XZ = 0.01f;
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

            _flyVisualEffect.Toggle(_currentMoveState == EMovementState.Flying);

            _lastMoveState = _currentMoveState;
        }

        private bool _jumpInitiated; // Tracks if a jump is pending until FixedUpdate applies it
        private bool IsGrounded()
        {
            bool isGrounded = CC.isGrounded;

            if (_jumpInitiated)
                return false;

            return isGrounded;
        }
    }
}