using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterAnimatorEvents : MonoBehaviour
    {
        [SerializeField]
        private NonPlayerCharacter _npc;

        public void FootR()
        { }

        public void FootL()
        { }

        public void Hit()
        {
            Debug.Log("Hit");
            _npc.Brain.OnHitFromAnimation();
        }
    }
}
