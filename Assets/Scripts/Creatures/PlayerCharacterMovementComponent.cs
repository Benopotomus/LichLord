using UnityEngine;
using Fusion;

namespace LichLord
{
    public enum EMovementState : byte
    {
        Grounded,
        Jumping,
        Flying
    }

    public class PlayerCharacterMovementComponent : NetworkBehaviour
    {
        [SerializeField]
        private PlayerCharacter _playerCharacter;
        public PlayerCharacter PC => _playerCharacter;

        [SerializeField]
        private CharacterController CC;

        [Networked, OnChangedRender(nameof(OnRep_CurrentMovementState))]
        private EMovementState CurrentMovementState { get; set; }

        [Header("References")]
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
        public float FlyVerticalBraking = 4f;
        public float FlyHorizontalSpeed = 7f;

        [Header("Jump Setup")]
        public float JumpImpulse = 5f;
        public float JumpHoldVelocity = 5.5f;
        public float JumpBufferTime = 0.2f;

        private float _jumpBufferTimer;
        private bool _jumpInputBuffered;
        private Vector3 _localVelocity;
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

        [Networked]
        private ref FWorldTransform _worldTransform => ref MakeRef<FWorldTransform>();
        public FWorldTransform WorldTransform => _worldTransform;

        [Networked]
        private ref FVelocity _worldVelocity => ref MakeRef<FVelocity>();

        [Networked]
        private NetworkBool _isGrounded { get; set; }

        private float _speed => _worldVelocity.Velocity.magnitude;

        public override void Spawned()
        {
            base.Spawned();
            CurrentMovementState = EMovementState.Grounded;
            _jumpCount = 0;
            _localVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _jumpInputBuffered = false;
            _castSpeedMultiplier = 1f;
            _currentMoveSpeed = WalkSpeed;
        }

        public void Respawn()
        {
            _localVelocity = Vector3.zero;
            CurrentMovementState = EMovementState.Grounded;
            _jumpCount = 0;
            _jumpBufferTimer = 0f;
            _jumpInputBuffered = false;
            _castSpeedMultiplier = 1f;
            _currentMoveSpeed = WalkSpeed;
        }

        public void OnRender()
        {
            float deltaTime = Time.deltaTime;
            if (!HasStateAuthority)
            {
                CC.transform.position = Vector3.Lerp(CC.transform.position, _worldTransform.Position, deltaTime * 8);
                CC.transform.rotation = Quaternion.Lerp(CC.transform.rotation, Quaternion.Euler(0f, _worldTransform.Yaw, 0f), deltaTime * 8f);
            }

            var moveSpeed = transform.InverseTransformVector(_worldVelocity.Velocity);

            Animator.SetFloat(_animIDSpeedX, moveSpeed.x, 0.1f, deltaTime);
            Animator.SetFloat(_animIDSpeedZ, moveSpeed.z, 0.1f, deltaTime);
            Animator.SetBool(_animIDGrounded, _isGrounded);
            Animator.SetFloat(_animIDPitch, _worldTransform.Pitch, 0.02f, deltaTime);

            FootstepSound.enabled = _isGrounded && _speed > 1f;
            ScalingRoot.localScale = Vector3.Lerp(ScalingRoot.localScale, Vector3.one, deltaTime * 8f);

            var emission = DustParticles.emission;
            emission.enabled = _isGrounded && _speed > 1f;
        }

        public void SetCastSpeedMultiplier(float multiplier)
        {
            _castSpeedMultiplier = multiplier;
        }

        public void SetLookRotation(FGameplayInput input)
        {
            _worldTransform.Yaw = input.LookRotation.y;
            _worldTransform.Pitch = Mathf.Clamp(input.LookRotation.x, -90, 90);
            CC.transform.rotation = Quaternion.Euler(0f, input.LookRotation.y, 0f);
        }

        public void ProcessInput(FGameplayInput input)
        {
            float deltaTime = Runner.DeltaTime;

            SetLookRotation(input);

            _isGrounded = CC.isGrounded;
            float gravity = DownGravity;

            var moveDirection = CC.transform.rotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);

            float targetSpeed = input.Sprint ? SprintSpeed : WalkSpeed;
            if (CurrentMovementState == EMovementState.Flying)
                targetSpeed = FlyHorizontalSpeed;

            float lerpSpeed = Mathf.Lerp(_currentMoveSpeed, targetSpeed, deltaTime / SprintAccelerationTime);
            _currentMoveSpeed = lerpSpeed;
            float currentSpeed = _currentMoveSpeed * _castSpeedMultiplier;
            var desiredMoveVelocity = moveDirection * currentSpeed;

            float acceleration = desiredMoveVelocity == Vector3.zero
                ? (CC.isGrounded ? GroundDeceleration : AirDeceleration)
                : (CC.isGrounded ? GroundAcceleration : AirAcceleration);

            Vector3 currentHorizontalVelocity = new Vector3(_localVelocity.x, 0f, _localVelocity.z);
            Vector3 newHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, desiredMoveVelocity, acceleration * deltaTime);
            _localVelocity = new Vector3(newHorizontalVelocity.x, _localVelocity.y, newHorizontalVelocity.z);

            Vector3 horizontalVelocity = new Vector3(_localVelocity.x, 0f, _localVelocity.z);
            bool isRising = CC.velocity.y > 0f;

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

            switch (CurrentMovementState)
            {
                case EMovementState.Grounded:
                    gravity = isRising ? UpGravity : DownGravity;
                    _verticalInput = 0;
                    _jumpCount = 0;

                    if ((_jumpInputBuffered || input.Jump) && _isGrounded)
                    {
                        _localVelocity.y = JumpImpulse; // Apply jump here
                        _jumpCount = 1;
                        CurrentMovementState = EMovementState.Jumping;
                        _jumpInputBuffered = false;
                        if (JumpAudioClip != null)
                            FootstepSound.PlayOneShot(JumpAudioClip);
                    }
                    else
                    {
                        _localVelocity.y += gravity * deltaTime;
                    }

                    CC.Move(_localVelocity * deltaTime);
                    break;

                case EMovementState.Jumping:
                    gravity = isRising ? UpGravity : DownGravity;

                    if ((_jumpInputBuffered || input.Jump) && _jumpCount < 2)
                    {
                        _localVelocity.y = JumpImpulse; // Apply double jump here
                        _jumpCount = 2;
                        CurrentMovementState = EMovementState.Flying;
                        _verticalInput = FlyAscendSpeed;
                        _jumpInputBuffered = false;
                        gravity = 0;
                    }
                    else
                    {
                        _localVelocity.y += gravity * deltaTime;
                    }

                    if (_isGrounded)
                    {
                        CurrentMovementState = EMovementState.Grounded;
                        _localVelocity.y = 0f;
                        _jumpCount = 0;
                        _jumpInputBuffered = false;
                        if (LandAudioClip != null)
                            FootstepSound.PlayOneShot(LandAudioClip);
                    }

                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, SprintSpeed * _castSpeedMultiplier);
                    _localVelocity = new Vector3(horizontalVelocity.x, _localVelocity.y, horizontalVelocity.z);
                    CC.Move(_localVelocity * deltaTime);
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
                    _localVelocity = new Vector3(horizontalVelocity.x, _verticalInput, horizontalVelocity.z);

                    CC.Move(_localVelocity * deltaTime);

                    if (_isGrounded)
                    {
                        CurrentMovementState = EMovementState.Grounded;
                        _localVelocity.y = 0f;
                        _jumpCount = 0;
                        _jumpInputBuffered = false;
                        if (LandAudioClip != null)
                        {
                            FootstepSound.PlayOneShot(LandAudioClip);
                        }
                    }

                    break;
            }

            WriteData();
        }

        private void WriteData()
        {

            // Update the runtime state
            // Update position only if the change is significant
            const float POSITION_THRESHOLD = 0.1f;
            if (Mathf.Abs(PC.CachedTransform.position.x - _worldTransform.PositionX) > POSITION_THRESHOLD)
            {
                _worldTransform.PositionX = PC.CachedTransform.position.x;
            }

            if (Mathf.Abs(PC.CachedTransform.position.y - _worldTransform.PositionY) > POSITION_THRESHOLD)
            {
                _worldTransform.PositionY = PC.CachedTransform.position.y;
            }

            if (Mathf.Abs(PC.CachedTransform.position.z - _worldTransform.PositionZ) > POSITION_THRESHOLD)
            {
                _worldTransform.PositionZ = PC.CachedTransform.position.z;
            }


            _worldVelocity.Velocity = CC.velocity;
        }

        private void OnRep_CurrentMovementState()
        {
        }
    }
}