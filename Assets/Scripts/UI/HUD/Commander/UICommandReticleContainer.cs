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

        private PlayerCharacter _pc;

        protected override void OnVisible()
        {
            base.OnVisible();
            StartCoroutine(BindPlayerCharacter());
        }

        private IEnumerator BindPlayerCharacter()
        {
            if (Context.LocalPlayerCharacter == null)
                yield return null;

            _pc = Context.LocalPlayerCharacter;

            for (int i = 0; i < _stanceSquadSlots.Length; i++)
            {
                UICommandSquadStanceSlot widget = _stanceSquadSlots[i];
            }

            OnIsModifyingStanceChanged(0, false);
            _pc.Commander.OnIsModifyingStanceChanged += OnIsModifyingStanceChanged;
            _pc.Commander.OnDesiredSquadStanceChanged += OnDesiredSquadStanceChanged;
        }

        private void OnIsModifyingStanceChanged(int squadId, bool isModifying)
        {
            if (isModifying)
            {
                _border.enabled = true;
                _stanceText.enabled = true;
                SetStanceText(squadId);

                for (int i = 0; i < _stanceSquadSlots.Length; i++)
                {
                    _stanceSquadSlots[i].ToggleVisibility(true);
                    _stanceSquadSlots[i].SetSelected(_pc.Commander.GetDesiredStance(squadId));

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

        private void OnDesiredSquadStanceChanged(int squadId, ESquadStance stance)
        {
            SetStanceText(squadId);

            for (int i = 0; i < _stanceSquadSlots.Length; i++)
            {
                UICommandSquadStanceSlot widget = _stanceSquadSlots[i];
                widget.SetSelected(stance);
            }
        }

        private void SetStanceText(int squadId)
        {
            switch (squadId)
            {
                case 0:
                    _stanceText.text = _pc.Commander.DesiredStance_0.ToString();
                    return;
                case 1:
                    _stanceText.text = _pc.Commander.DesiredStance_1.ToString();
                    return;
                case 2:
                    _stanceText.text = _pc.Commander.DesiredStance_2.ToString();
                    return;

            }
            _stanceText.text = "";
        }
    }
}
