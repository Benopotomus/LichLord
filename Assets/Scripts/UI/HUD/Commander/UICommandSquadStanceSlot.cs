
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UICommandSquadStanceSlot : UIWidget
    {
        [SerializeField] ESquadStance _squadStance;
        [SerializeField] protected Image _border;
        [SerializeField] protected Image _icon;

        public void ToggleVisibility(bool isVisible)
        {
            _border.enabled = isVisible;
            _icon.enabled = isVisible;
        }

        public void SetSelected(ESquadStance stance)
        {
            if (stance != _squadStance)
            {
                _border.enabled = false;
            }
            else
            {
                _border.enabled = true;
            }
        }

    }
}
