using JetBrains.Annotations;
using UnityEngine;

namespace LichLord.Items
{
    [CreateAssetMenu(menuName = "LichLord/Items/WeaponDefinition")]
    public class WeaponDefinition : ItemDefinition
    {
        [SerializeField]
        private int _idleAnimationId = 0;
        public int IdleAnimationID => _idleAnimationId;
    }
}
