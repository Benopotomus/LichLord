
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

        [SerializeField] private UIFloatingLifetimeBar _lifetimebar;

        public void SetNpcData(NonPlayerCharacter npc)
        {
            _npc = npc;

            _healthbar.SetHealth(npc.RuntimeState.GetHealth(), npc.RuntimeState.GetMaxHealth());

            if (npc.RuntimeState.IsCommandedUnit())
            {
                _lifetimebar.SetActive(true);
                _lifetimebar.SetLifetime(npc.RuntimeState.GetLifetimeProgress(), npc.RuntimeState.GetLifetimeProgressMax());
            }
            else
            {
                _lifetimebar.SetActive(false);
            }

        }
    }
}
