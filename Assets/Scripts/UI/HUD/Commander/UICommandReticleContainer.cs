using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LichLord.UI
{
    public class UICommandReticleContainer : UIWidget
    {
        [SerializeField] UICommandSquadStanceSlot[] _stanceSquadSlots;
        [SerializeField] protected Image _border;
        [SerializeField] private TextMeshProUGUI _stanceText;

        public void OnIsModifyingStanceChanged(PlayerCharacter pc, int squadId, bool isModifying)
        {
            if (isModifying)
            {
                _border.enabled = true;
                _stanceText.enabled = true;
                SetStanceText(pc, squadId);

                for (int i = 0; i < _stanceSquadSlots.Length; i++)
                {
                    _stanceSquadSlots[i].ToggleVisibility(true);
                    _stanceSquadSlots[i].SetSelected(pc.Commander.GetDesiredStance(squadId));

                }
            }
            else
            {
                _border.enabled = false;
                _stanceText.enabled = false;
                for (int i = 0; i < _stanceSquadSlots.Length; i++)
                    _stanceSquadSlots[i].ToggleVisibility(false);
            }
        }

        public void OnDesiredSquadStanceChanged(PlayerCharacter pc, int squadId, ESquadStance stance)
        {
            SetStanceText(pc, squadId);

            for (int i = 0; i < _stanceSquadSlots.Length; i++)
            {
                UICommandSquadStanceSlot widget = _stanceSquadSlots[i];
                widget.SetSelected(stance);
            }
        }

        private void SetStanceText(PlayerCharacter pc, int squadId)
        {
            switch (squadId)
            {
                case 0:
                    _stanceText.text = pc.Commander.DesiredStance_0.ToString();
                    return;
                case 1:
                    _stanceText.text = pc.Commander.DesiredStance_1.ToString();
                    return;
                case 2:
                    _stanceText.text = pc.Commander.DesiredStance_2.ToString();
                    return;

            }
            _stanceText.text = "";
        }
    }
}
