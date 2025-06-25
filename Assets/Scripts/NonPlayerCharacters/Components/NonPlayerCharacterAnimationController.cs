using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterAnimationController : MonoBehaviour
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

        [SerializeField] private NonPlayerCharacter _npc;
        [SerializeField] private Animator _animator;

        public void SetAnimationForTrigger(FAnimationTrigger animationTrigger)
        {
            _animator.SetInteger(_animIDAction, animationTrigger.Action);
            _animator.SetBool(_animIDMoving, animationTrigger.IsMoving);
            _animator.SetBool(_animIDBlocking, animationTrigger.IsBlocking);
            _animator.SetInteger(_animIDWeapon, _npc.Weapons.GetWeaponID());
            _animator.SetInteger(_animIDRightWeapon, animationTrigger.RightWeapon);
            _animator.SetInteger(_animIDSide, animationTrigger.Side);
            _animator.SetInteger(_animIDJumping, animationTrigger.Jumping);
            _animator.SetInteger(_animIDTriggerNumber, animationTrigger.TriggerNumber);
            _animator.SetTrigger(_animIDTrigger);
        }

        public void SetAnimationForState(ENonPlayerState state)
        {
            switch (state)
            {
                case ENonPlayerState.Idle:
                    _animator.SetInteger(_animIDWeapon, _npc.Weapons.GetWeaponID());
                    _animator.SetInteger(_animIDTriggerNumber, 25);
                    _animator.SetTrigger(_animIDTrigger);
                    break;

                case ENonPlayerState.Dead:
                    _animator.SetInteger(_animIDWeapon, _npc.Weapons.GetWeaponID());
                    _animator.SetInteger(_animIDTriggerNumber, 20);
                    _animator.SetTrigger(_animIDTrigger);
                    break;
            }
        }

        public void UpdateAnimatonForMovement(ref FNonPlayerCharacterData data, Vector3 localVelocity, float renderDeltaTime)
        {
            bool isMoving = localVelocity.magnitude > 0.1f;

            if (_npc.State.CurrentState != ENonPlayerState.Idle)
                isMoving = false;

            Vector3 animationVelocity = localVelocity / _npc.GetDefinition(ref data).WalkSpeed;
            _animator.SetBool(_animIDMoving, isMoving);
            _animator.SetFloat(_animIDSpeedX, animationVelocity.x, 0.1f, renderDeltaTime);
            _animator.SetFloat(_animIDSpeedZ, animationVelocity.z, 0.1f, renderDeltaTime);
        }
    }
}
