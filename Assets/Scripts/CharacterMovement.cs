using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

namespace LichLord
{
    public enum MovementState : byte
    {
        Grounded,
        Jumping,
        Flying
    }

    public class CharacterMovement : NetworkBehaviour
    {
        [Networked, OnChangedRender(nameof(OnRep_CurrentMovementState))]
        private MovementState CurrentMovementState { get; set; }

        [Header("References")]
        public SimpleKCC KCC;
        public Animator Animator;
        public PlayerCharacterInput PlayerInput;
        public Transform ScalingRoot;

        [Header("Movement Setup")]
        public float WalkSpeed = 2f;
        public float SprintSpeed = 5f;
        public float SprintAccelerationTime = 0.3f; // Time to transition between walk and sprint speeds
        public float UpGravity = -10f;
        public float DownGravity = -20f;
        public float RotationSpeed = 8f;

        [Header("Movement Accelerations")]
        public float GroundAcceleration = 55f;
        public float GroundDeceleration = 25f;
        public float AirAcceleration = 25f;
        public float AirDeceleration = 1.3f;

        [Header("Flying Setup")]
        public float FlyAscendSpeed = 1f;
        public float FlyDescendSpeed = 1f;
        public float FlyAscendAcceleration = 4f;
        public float FlyDescendAcceleration = 4f;
        public float FlyVerticalBraking = 4f;
        public float FlyHorizontalSpeed = 4f;

        [Header("Jump Setup")]
        public float JumpImpulse = 3f;
        public float JumpHoldVelocity = 5.5f; 
        public float FlyHoldThreshold = 0.75f;
        public float JumpBufferTime = 0.2f; // Grace period for jump input

        private float _jumpHeldTime;
        private Vector3 _moveVelocity;
        private float _verticalInput = 0f;
        private float _jumpBufferTimer = 0f;
        private bool _jumpInputBuffered = false;
        private float _castSpeedMultiplier = 1f; // Multiplier for movement speed during actions
        private float _currentMoveSpeed; // Tracks current movement speed for sprint lerp

        private int _animIDSpeedX = Animator.StringToHash("SpeedX");
        private int _animIDSpeedZ = Animator.StringToHash("SpeedZ");
        private int _animIDPitch = Animator.StringToHash("Pitch");
        private int _animIDGrounded = Animator.StringToHash("Grounded");
        private int _animIDJump = Animator.StringToHash("Jump");
        private int _animIDFreeFall = Animator.StringToHash("FreeFall");

        public AudioSource FootstepSound;
        public AudioClip JumpAudioClip;
        public AudioClip LandAudioClip;

        [Header("VFX")]
        public ParticleSystem DustParticles;

        public override void Spawned()
        {
            base.Spawned();
            KCC.Settings.ForcePredictedLookRotation = true;
            CurrentMovementState = MovementState.Grounded;
            _jumpHeldTime = 0f;
            _moveVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _jumpInputBuffered = false;
            _castSpeedMultiplier = 1f;
            _currentMoveSpeed = WalkSpeed; // Initialize to walk speed
        }

        public void Respawn()
        {
            _moveVelocity = Vector3.zero;
            CurrentMovementState = MovementState.Grounded;
            _jumpHeldTime = 0f;
            _jumpBufferTimer = 0f;
            _jumpInputBuffered = false;
            _castSpeedMultiplier = 1f;
            _currentMoveSpeed = WalkSpeed; // Reset to walk speed
        }

        public override void Render()
        {
            if (HasStateAuthority && PlayerInput != null)
            {
                KCC.SetLookRotation(PlayerInput.CurrentInput.LookRotation, -90f, 90f);
            }

            var moveSpeed = transform.InverseTransformVector(KCC.RealVelocity);

            Animator.SetFloat(_animIDSpeedX, moveSpeed.x, 0.1f, Time.deltaTime);
            Animator.SetFloat(_animIDSpeedZ, moveSpeed.z, 0.1f, Time.deltaTime);
            Animator.SetBool(_animIDGrounded, KCC.IsGrounded);
            Animator.SetFloat(_animIDPitch, KCC.GetLookRotation(true, false).x, 0.02f, Time.deltaTime);

            FootstepSound.enabled = KCC.IsGrounded && KCC.RealSpeed > 1f;
            ScalingRoot.localScale = Vector3.Lerp(ScalingRoot.localScale, Vector3.one, Time.deltaTime * 8f);

            var emission = DustParticles.emission;
            emission.enabled = KCC.IsGrounded && KCC.RealSpeed > 1f;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || PlayerInput == null)
            {
                return;
            }

            ProcessMovementInput(PlayerInput.CurrentInput);
        }

        public void SetCastSpeedMultiplier(float multiplier)
        {
            _castSpeedMultiplier = multiplier;
        }

        public void ProcessMovementInput(GameplayInput input)
        {
            KCC.SetLookRotation(input.LookRotation, -90f, 90f);

            var moveDirection = KCC.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);

            // Lerp current move speed toward target speed (WalkSpeed or SprintSpeed)
            float targetSpeed = input.Sprint ? SprintSpeed : WalkSpeed;
            float lerpSpeed = Mathf.Lerp(_currentMoveSpeed, targetSpeed, Runner.DeltaTime / SprintAccelerationTime);
            _currentMoveSpeed = lerpSpeed;
            float currentSpeed = _currentMoveSpeed * _castSpeedMultiplier;
            var desiredMoveVelocity = moveDirection * currentSpeed;

            float acceleration = desiredMoveVelocity == Vector3.zero
                ? (KCC.IsGrounded ? GroundDeceleration : AirDeceleration)
                : (KCC.IsGrounded ? GroundAcceleration : AirAcceleration);

            _moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);

            Vector3 horizontalVelocity = new Vector3(_moveVelocity.x, 0f, _moveVelocity.z);
            float jumpImpulse = 0f;
            bool isRising = KCC.RealVelocity.y > 0f;

            // Update jump buffer timer
            if (input.Jump && !input.JumpHeld)
            {
                _jumpInputBuffered = true;
                _jumpBufferTimer = JumpBufferTime;
            }

            if (_jumpInputBuffered)
            {
                _jumpBufferTimer -= Runner.DeltaTime;
                if (_jumpBufferTimer <= 0f)
                {
                    _jumpInputBuffered = false;
                }
            }

            switch (CurrentMovementState)
            {
                case MovementState.Grounded:
                    KCC.SetGravity(isRising ? UpGravity : DownGravity);
                    _verticalInput = 0;

                    if (KCC.ProjectOnGround(_moveVelocity, out var projectedVector))
                    {
                        _moveVelocity = projectedVector;
                    }

                    // Check for jump input or buffered jump
                    if ((_jumpInputBuffered || input.Jump) && KCC.IsGrounded)
                    {
                        jumpImpulse = JumpImpulse;
                        _jumpHeldTime = 0f;
                        CurrentMovementState = MovementState.Jumping;
                        _jumpInputBuffered = false; // Clear buffer after jump
                        if (JumpAudioClip != null)
                        {
                            FootstepSound.PlayOneShot(JumpAudioClip);
                        }
                    }

                    KCC.Move(_moveVelocity, jumpImpulse);
                    break;

                case MovementState.Jumping:
                    KCC.SetGravity(isRising ? UpGravity : DownGravity);

                    if (input.JumpHeld)
                    {
                        _verticalInput += (JumpHoldVelocity * Runner.DeltaTime);
                        _jumpHeldTime += Runner.DeltaTime;
                    }

                    if (!KCC.IsGrounded && _jumpHeldTime >= FlyHoldThreshold)
                    {
                        CurrentMovementState = MovementState.Flying;
                        _verticalInput = FlyAscendSpeed;
                        _moveVelocity.y = _verticalInput;
                        _jumpHeldTime = 0f;
                        KCC.SetGravity(0f);
                        KCC.ResetVelocity();
                        KCC.Move(_moveVelocity, 0f);
                        break;
                    }

                    if (KCC.IsGrounded)
                    {
                        CurrentMovementState = MovementState.Grounded;
                        _moveVelocity.y = 0f;
                        _jumpHeldTime = 0f;
                        _jumpInputBuffered = false; // Clear buffer on landing
                        if (LandAudioClip != null)
                        {
                            FootstepSound.PlayOneShot(LandAudioClip);
                        }
                    }

                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, SprintSpeed * _castSpeedMultiplier);
                    _moveVelocity = new Vector3(horizontalVelocity.x, _verticalInput, horizontalVelocity.z);

                    KCC.Move(_moveVelocity, 0f);
                    break;

                case MovementState.Flying:
                    if (input.JumpHeld)
                    {
                        _verticalInput = Mathf.Lerp(_verticalInput, FlyAscendSpeed, FlyAscendAcceleration * Runner.DeltaTime);
                    }
                    else if (input.CrouchHeld)
                    {
                        _verticalInput = Mathf.Lerp(_verticalInput, -FlyDescendSpeed, FlyDescendAcceleration * Runner.DeltaTime);
                    }
                    else
                    {
                        _verticalInput = Mathf.Lerp(_verticalInput, 0, FlyVerticalBraking * Runner.DeltaTime);
                    }

                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, FlyHorizontalSpeed * _castSpeedMultiplier);
                    _moveVelocity = new Vector3(horizontalVelocity.x, _verticalInput, horizontalVelocity.z);

                    KCC.Move(_moveVelocity, 0f);

                    if (KCC.IsGrounded)
                    {
                        CurrentMovementState = MovementState.Grounded;
                        _moveVelocity.y = 0f;
                        _jumpInputBuffered = false; // Clear buffer on landing
                        if (LandAudioClip != null)
                        {
                            FootstepSound.PlayOneShot(LandAudioClip);
                        }
                    }

                    break;
            }
        }

        private void OnRep_CurrentMovementState()
        {
        }
    }
}