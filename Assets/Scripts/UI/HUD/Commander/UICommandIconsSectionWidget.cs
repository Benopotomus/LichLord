
using System.Collections;
using UnityEngine;

namespace LichLord.UI
{
    public class UICommandIconsSectionWidget : UIWidget
    {

        [SerializeField]
        private PlayerCharacter _pc;

        [SerializeField]
        private UISquadCommandSlot[] _slotWidgets;

        protected override void OnVisible()
        {
            base.OnVisible();
            StartCoroutine(BindPlayerCharacter());
        }

        protected override void OnHidden()
        {
            if (_pc != null)
                _pc.Commander.OnCommandSquadUnitChanged -= OnCommandUnitSquadChanged;

            base.OnHidden();
        }

        private IEnumerator BindPlayerCharacter()
        {
            if (Context.LocalPlayerCharacter == null)
                yield return null;

            _pc = Context.LocalPlayerCharacter;

            for (int i = 0; i < _slotWidgets.Length; i++)
            {
                UISquadCommandSlot widget = _slotWidgets[i];
                CommandSquad squad = _pc.Commander.Squads[i];

            }

            _pc.Commander.OnCommandSquadUnitChanged += OnCommandUnitSquadChanged;
        }

        private void OnCommandUnitSquadChanged(int squadId, CommandSquad squad, int formationIndex)
        {
            UISquadCommandSlot widget = _slotWidgets[squadId];

            widget.OnCommandSquadChanged(squadId, squad, formationIndex);

        }
    }
}
