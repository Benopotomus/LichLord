using LichLord.NonPlayerCharacters;
using UnityEngine;

namespace LichLord.Items
{
    [CreateAssetMenu(menuName = "LichLord/Items/SummonableItemDefinition")]
    public class SummonableItemDefinition : ItemDefinition
    {
        [SerializeField]
        private ManeuverDefinition _maneuverDefinition;
        public ManeuverDefinition ManeuverDefinition => _maneuverDefinition;


        [SerializeField]
        private NonPlayerCharacterDefinition _nonPlayerCharacterDefinition;
        public NonPlayerCharacterDefinition NonPlayerCharacterDefinition => _nonPlayerCharacterDefinition;
    }
}
