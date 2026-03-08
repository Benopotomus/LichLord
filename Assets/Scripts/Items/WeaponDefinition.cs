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

        [SerializeField]
        protected Vector3 _upperArmOffsetEuler = new Vector3(0f, 0f, 0);
        public Vector3 UpperArmOffsetEuler => _upperArmOffsetEuler;

        [SerializeField]
        protected Vector3 _lowerArmOffsetEuler = new Vector3(0f, 0f, 0);
        public Vector3 LowerArmOffsetEuler => _lowerArmOffsetEuler;
    }
}
