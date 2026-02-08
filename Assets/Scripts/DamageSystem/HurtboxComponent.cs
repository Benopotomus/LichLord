
using UnityEngine;

namespace LichLord
{
    public class HurtboxComponent : MonoBehaviour
    {
        [SerializeField] protected Collider[] _hurtBoxes;
        public Collider[] HurtBoxes => _hurtBoxes;

        public void SetHurtBoxesActive(bool newIsActive)
        {
            foreach (var hitbox in _hurtBoxes)
                hitbox.enabled = newIsActive;
        }
    }
}
