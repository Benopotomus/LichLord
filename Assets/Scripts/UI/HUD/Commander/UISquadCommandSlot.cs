
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

        public void OnCommandSquadChanged(int squadId, CommandSquad squad, int formationIndex)
        {
            if (squad.HasAnyUnitsActive())
            {
                _cooldownImage.enabled = true;
            }
            else
            {
                _cooldownImage.enabled = false;
            }


        }

    }
}
