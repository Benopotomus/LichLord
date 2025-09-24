using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord
{
    public class PlayerCharacterAnimatorEvents : MonoBehaviour
    {
        [SerializeField]
        private PlayerCharacter _pc;

        public void FootR()
        { }

        public void FootL()
        { }

        public void Hit()
        {
            Debug.Log("Hit");
            _pc.Weapons.OnHitFromAnimation();
        }

        public void Land() 
        { }

        public void Shoot()
        { }

        public void Shoot(float shooting, int shooting1, string shootingString, object shootObject)
        { }

        public void Shoot(string shooting)
        { }

        public void Shoot(object shootObject)
        { }

        public void Shoot(int shooting)
        { }

        public void WeaponSwitch(int newWeapon)
        {
            _pc.Weapons.WeaponSwitch();
        }
    }
}
