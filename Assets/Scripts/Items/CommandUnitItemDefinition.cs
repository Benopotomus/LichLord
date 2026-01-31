using LichLord.NonPlayerCharacters;
using UnityEngine;

namespace LichLord.Items
{
    [CreateAssetMenu(menuName = "LichLord/Items/CommandUnitItemDefinition")]
    public class CommandUnitItemDefinition : ItemDefinition
    {
        [SerializeField]
        private NonPlayerCharacterDefinition _nonPlayerCharacterDefinition;
        public NonPlayerCharacterDefinition NonPlayerCharacterDefinition => _nonPlayerCharacterDefinition;
    }
}
