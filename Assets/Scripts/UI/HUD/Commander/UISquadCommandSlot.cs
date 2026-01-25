
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UISquadCommandSlot : UIWidget
    {
        [SerializeField]
        private int _slot;

        [SerializeField]
        private TextMeshProUGUI _text;

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private Image _cooldownImage;

        [SerializeField]
        private Color _unselectedColor;

        [SerializeField]
        private Color _selectedColor;

        [SerializeField]
        private Color _activeColor;

        public void SetSlot(int slot)
        {
            _slot = slot;
            _text.text = (_slot + 1).ToString();
        }

        public void OnCommandSquadChanged(int squadId, CommandSquad squad, int formationIndex)
        {
            if (squadId != _slot)
                return;

            if (squad.HasAnyUnitsActive())
            {
                _cooldownImage.enabled = false;
            }
            else
            {
                _cooldownImage.enabled = true;
            }


        }

    }
}
