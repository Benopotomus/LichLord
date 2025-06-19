using UnityEngine;
using Fusion;
using System;

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
        public PlayerCharacter PC => _pc;

        [SerializeField]
        private CharacterController CC;

        [Networked]
        private EMovementState _currentMoveState { get; set; }
        public EMovementState CurrentMoveState => _currentMoveState;

        [SerializeField]
        private EMovementState _lastState;

        [Header("References")]
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

        private int _jumpCount { get; set; }

        private int _animIDSpeedX = Animator.StringToHash("Velocity X");
        private int _animIDSpeedZ = Animator.StringToHash("Velocity Z");
        private int _animIDMoving = Animator.StringToHash("Moving");
        private int _animIDJump = Animator.StringToHash("Jumping");
        private int _animIDTriggerNumber = Animator.StringToHash("TriggerNumber");

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

            if (HasStateAuthority)
                _currentMoveState = EMovementState.Walking;

            _jumpCount = 0;
            _localVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _jumpInputBuffered = false;
            _castSpeedMultiplier = 1f;
            _currentMoveSpeed = WalkSpeed;
        }

        public void OnRender(float deltaTime)
        {
            UpdateMovementState();
            UpdateAnimator(deltaTime);
            UpdateRemotePosition(deltaTime);

            FootstepSound.enabled = _isGrounded && _speed > 1f;
            //ScalingRoot.localScale = Vector3.Lerp(ScalingRoot.localScale, Vector3.one, deltaTime * 8f);

            var emission = DustParticles.emission;
        }

        private void UpdateRemotePosition(float deltaTime)
        {
            if (!HasStateAuthority)
            {
                Vector3 lastPos = CC.transform.position;

                float x = Mathf.Lerp(lastPos.x, _worldTransform.PositionX, deltaTime * 5f);
                float y = Mathf.Lerp(lastPos.y, _worldTransform.PositionY, deltaTime * 10f);
                float z = Mathf.Lerp(lastPos.z, _worldTransform.PositionZ, deltaTime * 5f);
                CC.transform.position = new Vector3(x, y, z);

                CC.transform.rotation = Quaternion.Lerp(CC.transform.rotation, Quaternion.Euler(0f, _worldTransform.Yaw, 0f), deltaTime * 8f);
            }
        }

        private void UpdateAnimator(float deltaTime)
        {
            var horiziontalVelocity = new Vector3(_worldVelocity.Velocity.x , 0, _worldVelocity.Velocity.z);
            var moveSpeed = transform.InverseTransformVector(horiziontalVelocity).normalized;

            switch (_currentMoveState)
            {
                case EMovementState.Walking:
                    PC.Animator.SetBool(_animIDMoving, true);
                    PC.Animator.SetFloat(_animIDSpeedX, moveSpeed.x, 0.1f, deltaTime);
                    PC.Animator.SetFloat(_animIDSpeedZ, moveSpeed.z, 0.1f, deltaTime);
                    break;
                case EMovementState.Jumping:
                    break;
                case EMovementState.Flying:
                    break;
            }
        }

        public void SetManeuverSpeedMultiplier(float multiplier)
        {
            _castSpeedMultiplier = multiplier;
        }

        public void SetLookRotation(FGameplayInput input)
        {
            _worldTransform.Yaw = input.LookRotation.y;
            _worldTransform.Pitch = Mathf.Clamp(input.LookRotation.x, -90, 90);
            CC.transform.rotation = Quaternion.Euler(0f, input.LookRotation.y, 0f);
        }

        public void OnFixedUpdate(ref FGameplayInput input)
        {
            float deltaTime = Runner.DeltaTime;

            SetLookRotation(input);

            _isGrounded = CC.isGrounded;

            if (_isGrounded)
                _currentMoveState = EMovementState.Walking;

            float gravity = DownGravity;

            var moveDirection = CC.transform.rotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);

            float targetSpeed = input.Sprint ? SprintSpeed : WalkSpeed;
            if (_currentMoveState == EMovementState.Flying)
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

            switch (_currentMoveState)
            {
                
                 case EMovementState.Walking:
                    gravity = DownGravity;
                    _verticalInput = 0;
                    _jumpCount = 0;

                    if ((_jumpInputBuffered || input.Jump) && _isGrounded)
                    {
                        _localVelocity.y = JumpImpulse; // Apply jump here
                        _jumpCount = 1;
                        _currentMoveState = EMovementState.Jumping;
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
                        _currentMoveState = EMovementState.Flying;
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
                        _currentMoveState = EMovementState.Walking;
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
                        _currentMoveState = EMovementState.Walking;
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
            const float POSITION_THRESHOLD = 0.05f;
            if (Mathf.Abs(PC.CachedTransform.position.x - _worldTransform.PositionX) > POSITION_THRESHOLD)
            {
                _worldTransform.PositionX = PC.CachedTransform.position.x;
            }

            const float POSITION_THRESHOLD_Y = 0.01f;
            if (Mathf.Abs(PC.CachedTransform.position.y - _worldTransform.PositionY) > POSITION_THRESHOLD_Y)
            {
                _worldTransform.PositionY = PC.CachedTransform.position.y;
            }

            if (Mathf.Abs(PC.CachedTransform.position.z - _worldTransform.PositionZ) > POSITION_THRESHOLD)
            {
                _worldTransform.PositionZ = PC.CachedTransform.position.z;
            }


            _worldVelocity.Velocity = CC.velocity;
        }

        private void UpdateMovementState()
        {
            if (_currentMoveState == _lastState)
                return;

            switch (_currentMoveState)
            {
                case EMovementState.Walking:
                    Debug.Log("Jump Land");
                    PC.Animator.SetBool(_animIDMoving, true);
                    PC.Animator.SetFloat(_animIDSpeedX, 0f);
                    PC.Animator.SetFloat(_animIDSpeedZ, 0f);
                    PC.Animator.SetInteger(_animIDJump, 0);
                    PC.Animator.SetTrigger("Trigger");
                    break;

                case EMovementState.Jumping:
                    Debug.Log("Jump Hit");
                    PC.Animator.SetInteger("Weapon", 0);
                    PC.Animator.SetBool(_animIDMoving, false);
                    PC.Animator.SetBool("Swimming", false);
                    PC.Animator.SetInteger(_animIDJump, 1);
                    PC.Animator.SetInteger(_animIDTriggerNumber, 18);
                    PC.Animator.SetTrigger("Trigger");

                    break;
                case EMovementState.Flying:
                    Debug.Log("Jump Hit");
                    PC.Animator.SetInteger("Weapon", 0);
                    PC.Animator.SetBool(_animIDMoving, false);
                    PC.Animator.SetBool("Swimming", false);
                    PC.Animator.SetInteger(_animIDJump, 2);
                    PC.Animator.SetInteger(_animIDTriggerNumber, 18);
                    PC.Animator.SetTrigger("Trigger");
                    break;
            }

            _lastState = _currentMoveState;
        }
    }
}