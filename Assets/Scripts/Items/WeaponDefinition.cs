using UnityEngine;

namespace LichLord.Items
{
    [CreateAssetMenu(menuName = "LichLord/Items/WeaponDefinition")]
    public class WeaponDefinition : ItemDefinition
    {
        [SerializeField]
        private int _idleAnimationId = 0;
        public int IdleAnimationID => _idleAnimationId;

        [SerializeField]
        private int _damage = 10;
        public int Damage => _damage;

        [SerializeField]
        protected LayerMask _overlapCollisionLayer;
        public LayerMask OverlapCollisionLayer => _overlapCollisionLayer;
    }
}
