using UnityEngine;

namespace LichLord.Items
{
    [CreateAssetMenu(menuName = "LichLord/Items/SummonableDefinition")]
    public class SummonableDefinition : ItemDefinition
    {
        [SerializeField]
        private ManeuverDefinition _maneuverDefinition;
        public ManeuverDefinition ManeuverDefinition => _maneuverDefinition;

    }
}
