using UnityEngine;
using Fusion;
using DG.Tweening; // kept if you use it elsewhere
using Fusion.Addons.KCC;

namespace LichLord
{
    public enum EMovementState : byte
    {
        None,
        Walking,
        Jumping,
        Flying,
    }

    [RequireComponent(typeof(KCC))]
    public class PlayerCharacterMovementComponent : ContextBehaviour // ← changed to NetworkBehaviour
    {
        // ──────────────────────────────────────────────────────────────
        // References
        // ──────────────────────────────────────────────────────────────

        [SerializeField] private PlayerCharacter _pc;

        private KCC _ncc; // Fusion wrapper
        public KCC CC => _ncc;

        [Networked] public EMovementState CurrentMoveState { get; set; }

        [SerializeField] private EMovementState _lastMoveState = EMovementState.None;

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
        public float JumpHoldVelocity = 5.5f; // unused in current logic — can add variable height
        public float JumpBufferTime = 0.2f;

        private float _jumpBufferTimer;
        private bool _jumpInputBuffered;
        private int _jumpCount;
        private bool _jumpInitiated;

        public AudioSource FootstepSound;
        public AudioClip JumpAudioClip;
        public AudioClip LandAudioClip;

        [Header("VFX")]
        public ParticleSystem DustParticles;
        [SerializeField] private VisualEffectBase _flyVisualEffect;

        // ──────────────────────────────────────────────────────────────
        // Networked & runtime fields (kept/adapted)
        // ──────────────────────────────────────────────────────────────

        [Networked] private ref FWorldTransform _worldTransform => ref MakeRef<FWorldTransform>();
        public FWorldTransform WorldTransform => _worldTransform;

        [SerializeField] private Vector3 _worldVelocity;
        public Vector3 WorldVelocity => _worldVelocity;

        private Vector3 _authorityMoveVelocity;
        private Vector3 _lastPosition;
        private Vector3 _localVelocity;
        private float _lastYaw;
        private float _yawVelocity;

        public Transform LookTarget { get; set; }

        private void Awake()
        {

        }

        public void OnSpawned()
        {
            return;
            if (Object.HasStateAuthority)
            {
                CurrentMoveState = EMovementState.Walking;
                _authorityMoveVelocity = Vector3.zero;
            }

            _jumpCount = 0;
            _localVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _jumpInputBuffered = false;
            _jumpInitiated = false;
        }

        public override void FixedUpdateNetwork()
        {
            /*
            if (!Object.HasStateAuthority) return;

            if (!_spawnComplete)
            {
                _spawnComplete = true;
                transform.position = SpawnForcedPosition;
                return;
            }

            _jumpInitiated = false;

            // Fetch your input here — replace with your actual input system
            FGameplayInput input = default; // ← TODO: Get real networked input!

            ProcessMovement(ref input, Runner.DeltaTime);


            */
            // Optional: update networked world transform if not using NetworkTransform
            //WritePosition();
        }

        public void ProcessInput(ref FGameplayInput input, float deltaTime)
        {
            return;
            bool isGrounded = IsGrounded();

            Vector3 lastHorizontalVelocity = new Vector3(_authorityMoveVelocity.x, 0f, _authorityMoveVelocity.z);
            float gravity = DownGravity;

            var inputDirection = transform.rotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);

            float targetSpeed = input.Sprint ? SprintSpeed : WalkSpeed;
            if (CurrentMoveState == EMovementState.Flying)
                targetSpeed = FlyHorizontalSpeed;

            if (inputDirection != Vector3.zero)
            {
                float dot = Vector3.Dot(inputDirection.normalized, transform.forward);
                float t = (dot + 1.0f) / 2.0f;
                float speedMultiplier = Mathf.SmoothStep(0.75f, 1.0f, t);
                targetSpeed *= speedMultiplier;
            }

            targetSpeed *= _pc.Maneuvers.GetMoveSpeedMultiplier();

            Vector3 desiredMoveVelocity = inputDirection * targetSpeed;

            float acceleration = desiredMoveVelocity == Vector3.zero
                ? (isGrounded ? GroundDeceleration : AirDeceleration)
                : (isGrounded ? GroundAcceleration : AirAcceleration);

            Vector3 newHorizontalVelocity = Vector3.Lerp(lastHorizontalVelocity, desiredMoveVelocity, acceleration * deltaTime);

            // Preserve vertical from last frame, update horizontal
            Vector3 moveVelocity = new Vector3(newHorizontalVelocity.x, _authorityMoveVelocity.y, newHorizontalVelocity.z);

            // Jump buffering
            if (input.Jump && !input.JumpHeld)
            {
                _jumpInputBuffered = true;
                _jumpBufferTimer = JumpBufferTime;
            }
            if (_jumpInputBuffered)
            {
                _jumpBufferTimer -= deltaTime;
                if (_jumpBufferTimer <= 0f)
                    _jumpInputBuffered = false;
            }

            bool isRising = _authorityMoveVelocity.y > 0f;

            switch (CurrentMoveState)
            {
                case EMovementState.Walking:
                    _jumpCount = 0;

                    if (isGrounded)
                    {
                        moveVelocity.y = gravity * deltaTime; // gentle downward to stick

                        if (_jumpInputBuffered || input.Jump)
                        {
                            moveVelocity.y = JumpImpulse;
                            _jumpCount = 1;
                            CurrentMoveState = EMovementState.Jumping;
                            _jumpInputBuffered = false;
                            _jumpInitiated = true;

                            if (JumpAudioClip != null)
                                FootstepSound?.PlayOneShot(JumpAudioClip);
                        }
                    }
                    else
                    {
                        moveVelocity.y += gravity * deltaTime;
                        moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);
                        _jumpCount = 1;
                        CurrentMoveState = EMovementState.Jumping;
                        _jumpInputBuffered = false;
                    }
                    break;

                case EMovementState.Jumping:
                    gravity = isRising ? UpGravity : DownGravity;

                    if ((_jumpInputBuffered || input.Jump) && _jumpCount < 2)
                    {
                        moveVelocity.y = JumpImpulse;
                        _jumpCount = 2;
                        CurrentMoveState = EMovementState.Flying;
                        _jumpInputBuffered = false;
                        gravity = 0f;
                    }
                    else
                    {
                        moveVelocity.y += gravity * deltaTime;
                        moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);
                    }

                    if (isGrounded)
                    {
                        moveVelocity.y += gravity * deltaTime; // optional extra stick
                        moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);
                        CurrentMoveState = EMovementState.Walking;
                        _jumpCount = 0;
                        _jumpInputBuffered = false;

                        if (LandAudioClip != null)
                            FootstepSound?.PlayOneShot(LandAudioClip);
                    }
                    break;

                case EMovementState.Flying:
                    // Horizontal preserved from earlier lerp
                    // Vertical control
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
                        moveVelocity.y = Mathf.Lerp(moveVelocity.y, 0f, FlyVerticalBraking * deltaTime);
                    }

                    if (isGrounded)
                    {
                        moveVelocity.y = gravity;
                        CurrentMoveState = EMovementState.Walking;
                    }
                    break;
            }

            _authorityMoveVelocity = moveVelocity;

            // Apply movement using Fusion's networked controller
            _ncc.SetKinematicVelocity(_authorityMoveVelocity);
        }

        private bool IsGrounded()
        {
            if (_jumpInitiated) return false;
            return CC.Data.IsGrounded;
        }

        // Keep your helper methods
        public void UpdateRemotePosition(float deltaTime)
        {
            /*
            if (Object.HasStateAuthority) return;

            Vector3 lastPos = transform.position;
            Vector3 newPosition = _worldTransform.Position;

            if ((newPosition - lastPos).sqrMagnitude > 36f)
            {
                transform.position = newPosition;
            }
            else
            {
                float x = Mathf.Lerp(lastPos.x, _worldTransform.PositionX, deltaTime * 5f);
                float y = Mathf.Lerp(lastPos.y, _worldTransform.PositionY, deltaTime * 10f);
                float z = Mathf.Lerp(lastPos.z, _worldTransform.PositionZ, deltaTime * 5f);
                transform.position = new Vector3(x, y, z);
            }

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, _worldTransform.Yaw, 0f), deltaTime * 8f);
            */
        }

        public void UpdateLookRotation(float deltaTime, float lerpSpeed)
        {
            return;
            // Your existing logic — uses transform now instead of CC.transform
            Quaternion targetRotation = LookTarget != null
                ? GetLookTargetRotation()
                : Context.Camera._cameraFollowTarget.rotation;

            float rawPitch = targetRotation.eulerAngles.x;
            float yaw = targetRotation.eulerAngles.y;
            float normalizedPitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;

            Quaternion currentRotation = transform.rotation;
            Quaternion targetYawRotation = Quaternion.Euler(0f, yaw, 0f);
            Quaternion lerpedRotation = Quaternion.Slerp(currentRotation, targetYawRotation, lerpSpeed * deltaTime);
            transform.rotation = lerpedRotation;

            _worldTransform.Yaw = lerpedRotation.eulerAngles.y;
            _worldTransform.Pitch = normalizedPitch;
        }

        public Quaternion GetLookTargetRotation()
        {
            Vector3 faceDirection = (LookTarget.position - transform.position).normalized;
            return Quaternion.LookRotation(faceDirection);
        }

        public void WritePosition(bool forceWrite = false)
        {
            return;
            // Your existing logic — now using transform
            const float POSITION_THRESHOLD_XZ = 0.01f;
            if (Mathf.Abs(transform.position.x - _worldTransform.PositionX) > POSITION_THRESHOLD_XZ || forceWrite)
                _worldTransform.PositionX = transform.position.x;

            if (Mathf.Abs(transform.position.z - _worldTransform.PositionZ) > POSITION_THRESHOLD_XZ || forceWrite)
                _worldTransform.PositionZ = transform.position.z;

            const float POSITION_THRESHOLD_Y = 0.01f;
            if (Mathf.Abs(transform.position.y - _worldTransform.PositionY) > POSITION_THRESHOLD_Y || forceWrite)
                _worldTransform.PositionY = transform.position.y;

            _worldTransform.Yaw = transform.eulerAngles.y;
        }

        private void UpdateVelocity(float renderDeltaTime)
        {
            return;
            _worldVelocity = Object.HasStateAuthority ? _authorityMoveVelocity : (transform.position - _lastPosition) / renderDeltaTime;
            _lastPosition = transform.position;
            _localVelocity = transform.InverseTransformDirection(_worldVelocity);
        }

        private void UpdateYawVelocity()
        {
            return;
            Vector3 forward = transform.forward;
            float currentYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _yawVelocity = currentYaw - _lastYaw;
            _lastYaw = currentYaw;
        }

        private void UpdateMovementState()
        {
            return;
            if (CurrentMoveState == _lastMoveState) return;
            _pc.AnimationController.OnMovementStateChanged(CurrentMoveState);
            _flyVisualEffect?.Toggle(CurrentMoveState == EMovementState.Flying);
            _lastMoveState = CurrentMoveState;
        }

        public void OnRender(float renderDeltaTime)
        {
            return;
            UpdateVelocity(renderDeltaTime);
            UpdateYawVelocity();
            UpdateMovementState();
            _pc.AnimationController.UpdateAnimatonForMovement(_localVelocity, _yawVelocity, CurrentMoveState, renderDeltaTime);
        }

        /// <summary>
        /// Special movement mode: smoothly lerp and brake toward an interactable position (e.g. during interaction animation or dialogue).
        /// Runs on authority only.
        /// </summary>
        /// <param name="interactablePosition">World position to approach</param>
        /// <param name="deltaTime">Should be Runner.DeltaTime when called from FixedUpdateNetwork</param>
        public void ProcessInteractMovement(Vector3 interactablePosition, float deltaTime)
        {
            return;
            if (!Object.HasStateAuthority)
                return;

            bool isGrounded = IsGrounded();
            float gravity = DownGravity;

            // Direction and desired speed (brake to 0)
            Vector3 moveDirection = (interactablePosition - transform.position).normalized;
            float targetSpeed = 0f; // we want to slow down to stop near the target

            Vector3 lastHorizontalVelocity = new Vector3(_authorityMoveVelocity.x, 0f, _authorityMoveVelocity.z);

            // Strong lerp toward zero speed (strong braking)
            float currentSpeed = Mathf.Lerp(lastHorizontalVelocity.magnitude, targetSpeed, 20f * deltaTime);

            Vector3 desiredMoveVelocity = moveDirection * currentSpeed;

            // Use normal acceleration rules (but very high deceleration is already in the lerp above)
            float acceleration = desiredMoveVelocity == Vector3.zero
                ? (isGrounded ? GroundDeceleration : AirDeceleration)
                : (isGrounded ? GroundAcceleration : AirAcceleration);

            Vector3 newHorizontalVelocity = Vector3.Lerp(lastHorizontalVelocity, desiredMoveVelocity, acceleration * deltaTime);

            // Preserve vertical velocity, update horizontal
            Vector3 moveVelocity = new Vector3(newHorizontalVelocity.x, _authorityMoveVelocity.y, newHorizontalVelocity.z);

            bool isRising = _authorityMoveVelocity.y > 0f;

            switch (CurrentMoveState)
            {
                case EMovementState.Walking:
                    if (isGrounded)
                    {
                        // Keep slight downward force to stay grounded
                        moveVelocity.y = gravity * deltaTime;
                    }
                    else
                    {
                        // Fall normally if airborne during interaction
                        moveVelocity.y += gravity * deltaTime;
                        moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);
                        _jumpCount = 1;
                        CurrentMoveState = EMovementState.Jumping;
                        _jumpInputBuffered = false;
                    }
                    break;

                case EMovementState.Jumping:
                    gravity = isRising ? UpGravity : DownGravity;

                    moveVelocity.y += gravity * deltaTime;
                    moveVelocity.y = Mathf.Max(TerminalVelocity, moveVelocity.y);

                    if (isGrounded)
                    {
                        CurrentMoveState = EMovementState.Walking;
                        _jumpCount = 0;
                        _jumpInputBuffered = false;

                        if (LandAudioClip != null)
                            FootstepSound?.PlayOneShot(LandAudioClip);
                    }
                    break;

                case EMovementState.Flying:
                    // Strong vertical brake during interaction (override flying controls)
                    moveVelocity.y = Mathf.Lerp(moveVelocity.y, 0f, 20f * deltaTime);

                    if (isGrounded)
                    {
                        moveVelocity.y = gravity * deltaTime;
                        CurrentMoveState = EMovementState.Walking;
                    }
                    break;
            }

            _authorityMoveVelocity = moveVelocity;
        }
    }
}