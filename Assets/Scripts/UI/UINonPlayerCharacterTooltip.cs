using LichLord.Buildables;
using LichLord.NonPlayerCharacters;
using TMPro;
using UnityEngine;

namespace LichLord.UI
{
    public class UINonPlayerCharacterTooltip : UIWidget
    {
        [SerializeField] private NonPlayerCharacter _npc;

        [SerializeField] private TextMeshProUGUI _stateText;

        [SerializeField] private UIFloatingHealthbar _healthbar;

        public void SetNpcData(NonPlayerCharacter npc)
        {
            _npc = npc;

            _healthbar.SetHealth(npc.RuntimeState.GetHealth(), npc.RuntimeState.GetMaxHealth());
        }
    }
}
