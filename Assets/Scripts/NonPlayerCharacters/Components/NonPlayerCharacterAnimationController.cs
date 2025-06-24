using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
