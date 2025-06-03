using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord
{
    public class HurtboxComponent : MonoBehaviour
    {
        [SerializeField] protected Collider[] _hurtBoxes;

        public void SetHitBoxesActive(bool newIsActive)
        {
            foreach (var hitbox in _hurtBoxes)
                hitbox.enabled = newIsActive;
        }
    }
}
