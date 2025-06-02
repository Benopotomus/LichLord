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

    public class CreatureMovement : NetworkBehaviour
    {
        [Networked, OnChangedRender(nameof(OnRep_CurrentMovementState))]
        private MovementState CurrentMovementState { get; set; }

        [Header("References")]
        public SimpleKCC KCC;
        public Animator Animator;
        public Transform ScalingRoot;

        [Header("Movement Setup")]
        public float WalkSpeed = 4f;
        public float SprintSpeed = 7f;
        public float SprintAccelerationTime = 0.3f;
        public float UpGravity = -10f;
        public float DownGravity = -20f;
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
        public float FlyVerticalBraking = 2f;
        public float FlyHorizontalSpeed = 7f;

        [Header("Jump Setup")]
        public float JumpImpulse = 5f;
        public float JumpHoldVelocity = 5.5f;
        public float JumpBufferTime = 0.2f;

        private float _jumpBufferTimer;
        private bool _jumpInputBuffered;
        private Vector3 _moveVelocity;
        private float _verticalInput;
        private float _castSpeedMultiplier = 1f;
        private float _currentMoveSpeed;
        private int _jumpCount; // Track number of jumps

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
            _jumpCount = 0;
            _moveVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _jumpInputBuffered = false;
            _castSpeedMultiplier = 1f;
            _currentMoveSpeed = WalkSpeed;
        }

        public void Respawn()
        {
            _moveVelocity = Vector3.zero;
            CurrentMovementState = MovementState.Grounded;
            _jumpCount = 0;
            _jumpBufferTimer = 0f;
            _jumpInputBuffered = false;
            _castSpeedMultiplier = 1f;
            _currentMoveSpeed = WalkSpeed;
        }

        public void OnRender()
        {
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

        public void SetCastSpeedMultiplier(float multiplier)
        {
            _castSpeedMultiplier = multiplier;
        }

        public void SetLookRotation(FGameplayInput input)
        {
            KCC.SetLookRotation(input.LookRotation, -90f, 90f);
        }

        public void ProcessInput(FGameplayInput input)
        {
            KCC.SetLookRotation(input.LookRotation, -90f, 90f);

            var moveDirection = KCC.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);

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
                    _jumpCount = 0; // Reset jump count when grounded

                    if (KCC.ProjectOnGround(_moveVelocity, out var projectedVector))
                    {
                        _moveVelocity = projectedVector;
                    }

                    if ((_jumpInputBuffered || input.Jump) && KCC.IsGrounded)
                    {
                        jumpImpulse = JumpImpulse;
                        _jumpCount = 1;
                        CurrentMovementState = MovementState.Jumping;
                        _jumpInputBuffered = false;
                        if (JumpAudioClip != null)
                        {
                            FootstepSound.PlayOneShot(JumpAudioClip);
                        }
                    }

                    KCC.Move(_moveVelocity, jumpImpulse);
                    break;

                case MovementState.Jumping:
                    KCC.SetGravity(isRising ? UpGravity : DownGravity);

                    if ((_jumpInputBuffered || input.Jump) && _jumpCount < 2)
                    {
                        jumpImpulse = JumpImpulse;
                        _jumpCount = 2;
                        CurrentMovementState = MovementState.Flying;
                        _verticalInput = FlyAscendSpeed;
                        _moveVelocity.y = _verticalInput;
                        _jumpInputBuffered = false;
                        KCC.SetGravity(0f);
                        KCC.ResetVelocity();
                        if (JumpAudioClip != null)
                        {
                            //FootstepSound.PlayOneShot(JumpAudioClip);
                        }
                    }

                    if (KCC.IsGrounded)
                    {
                        CurrentMovementState = MovementState.Grounded;
                        _moveVelocity.y = 0f;
                        _jumpCount = 0;
                        _jumpInputBuffered = false;
                        if (LandAudioClip != null)
                        {
                            FootstepSound.PlayOneShot(LandAudioClip);
                        }
                    }

                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, SprintSpeed * _castSpeedMultiplier);
                    _moveVelocity = new Vector3(horizontalVelocity.x, _verticalInput, horizontalVelocity.z);

                    KCC.Move(_moveVelocity, jumpImpulse);
                    break;

                case MovementState.Flying:
                    KCC.SetGravity(0f);
                    KCC.ResetVelocity();
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
                        _jumpCount = 0;
                        _jumpInputBuffered = false;
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