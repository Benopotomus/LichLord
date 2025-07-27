using UnityEngine;

namespace LichLord
{
    public class PlayerCharacterAnimationController : MonoBehaviour
    {

        private int _animIDMoving = Animator.StringToHash("Moving");
        private int _animIDBlocking = Animator.StringToHash("Blocking");
        private int _animIDWeapon = Animator.StringToHash("Weapon");
        private int _animIDTriggerNumber = Animator.StringToHash("TriggerNumber");
        private int _animIDTrigger = Animator.StringToHash("Trigger");
        private int _animIDRightWeapon = Animator.StringToHash("RightWeapon");
        private int _animIDSide = Animator.StringToHash("Side");
        private int _animIDJumping = Animator.StringToHash("Jumping");
        private int _animIDAction = Animator.StringToHash("Action");

        private int _animIDSpeedX = Animator.StringToHash("Velocity X");
        private int _animIDSpeedZ = Animator.StringToHash("Velocity Z");

        private int _animIDUpperBodyTriggerNumber = Animator.StringToHash("UpperBodyTriggerNumber");
        private int _animIDUpperBodyTrigger = Animator.StringToHash("UpperBodyTrigger");
        private int _animIDUpperBodyBlend = Animator.StringToHash("UpperBodyBlend");

        [SerializeField] private PlayerCharacter _pc;
        [SerializeField] private Animator _animator;

        public void SetAnimationForTrigger(FAnimationTrigger animationTrigger)
        {
            _animator.SetInteger(_animIDAction, animationTrigger.Action);
            _animator.SetBool(_animIDMoving, animationTrigger.IsMoving);
            _animator.SetBool(_animIDBlocking, animationTrigger.IsBlocking);
            _animator.SetInteger(_animIDWeapon, 0);
            _animator.SetInteger(_animIDRightWeapon, animationTrigger.RightWeapon);
            _animator.SetInteger(_animIDSide, animationTrigger.Side);
            _animator.SetInteger(_animIDJumping, animationTrigger.Jumping);
            _animator.SetInteger(_animIDTriggerNumber, animationTrigger.TriggerNumber);
            _animator.SetTrigger(_animIDTrigger);
        }

        public void UpdateAnimatonForMovement(Vector3 localVelocity, float yawVelocity, EMovementState moveState, float renderDeltaTime)
        {
            float speed = localVelocity.sqrMagnitude;
            float horizontalSpeed = new Vector3(localVelocity.x, 0, localVelocity.z).sqrMagnitude;

            float walkSpeed = 6f;

            // Determine if the character is moving
            bool isMoving = horizontalSpeed > 0.0f || Mathf.Abs(yawVelocity) > 1f;

            // Compute normalized animation velocity
            Vector3 animationVelocity = localVelocity / walkSpeed;

            // If movement is very small, zero forward motion and apply yaw as strafe
            if (horizontalSpeed < 0.01f)
            {
                animationVelocity.z = 0f;
                animationVelocity.x = Mathf.Clamp( yawVelocity * 4, -0.5f, 0.5f); // Turn in place animation
            }

            switch (moveState)
            {
                case EMovementState.Walking:
                    _animator.SetBool(_animIDMoving, isMoving);
                    _animator.SetFloat(_animIDSpeedX, animationVelocity.x, 0.25f, renderDeltaTime);
                    _animator.SetFloat(_animIDSpeedZ, animationVelocity.z, 0.25f, renderDeltaTime);
                    break;
                case EMovementState.Jumping:
                    break;
                case EMovementState.Flying:
                    break;
            }
        }

        public void OnMovementStateChanged(EMovementState newMovementState)
        {
            switch (newMovementState)
            {
                case EMovementState.Walking:
                    //Debug.Log("Walking");
                    _animator.SetBool(_animIDMoving, true);
                    _animator.SetFloat(_animIDSpeedX, 0f);
                    _animator.SetFloat(_animIDSpeedZ, 0f);
                    _animator.SetInteger(_animIDJumping, 0);
                    _animator.SetTrigger(_animIDTrigger);
                    break;

                case EMovementState.Jumping:
                    //Debug.Log("Jump Hit");
                    _animator.SetInteger(_animIDWeapon, 0);
                    _animator.SetBool(_animIDMoving, false);
                    _animator.SetInteger(_animIDJumping, 1);
                    _animator.SetInteger(_animIDTriggerNumber, 18);
                    _animator.SetTrigger(_animIDTrigger);

                    break;
                case EMovementState.Flying:
                    //Debug.Log("Jump Hit");
                    _animator.SetInteger(_animIDWeapon, 0);
                    _animator.SetBool(_animIDMoving, false);
                    _animator.SetInteger(_animIDJumping, 2);
                    _animator.SetInteger(_animIDTriggerNumber, 18);
                    _animator.SetTrigger(_animIDTrigger);
                    break;
            }
        }

        public void SetUpperBodyBlend(float blendAmount)
        {
            _animator.SetFloat(_animIDUpperBodyBlend, blendAmount);
        }

        public void SetAnimationForUpperBodyTrigger(int upperbodyTriggerNumber)
        {
            float blend = upperbodyTriggerNumber > 0 ? 0.01f : 0f;

            _animator.SetFloat(_animIDUpperBodyBlend, blend);
            _animator.SetInteger(_animIDUpperBodyTriggerNumber, upperbodyTriggerNumber);
            _animator.SetTrigger(_animIDUpperBodyTrigger);
        }

    }
}
