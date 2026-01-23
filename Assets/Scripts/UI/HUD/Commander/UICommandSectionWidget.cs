using LichLord.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.UI
{
    public class UICommandSectionWidget : UIWidget
    {

        [SerializeField]
        private CommanderComponent _commanderComponent;

        [SerializeField]
        UICommanderSquadWidget[] _squadWidgets;

        protected override void OnVisible()
        {
            base.OnVisible();
            StartCoroutine(BindCommanderComponent());
        }

        protected override void OnHidden()
        {
            if (_commanderComponent != null)
                _commanderComponent.OnCommandSquadUnitChanged -= OnCommandUnitSquadChanged;

            base.OnHidden();
        }

        private IEnumerator BindCommanderComponent()
        {
            if (Context.LocalPlayerCharacter == null)
                yield return null;

            _commanderComponent = Context.LocalPlayerCharacter.Commander;

            for (int i = 0; i < _squadWidgets.Length; i++)
            {
                UICommanderSquadWidget widget = _squadWidgets[i];
                CommandSquad squad = _commanderComponent.Squads[i];
                widget.UpdateAllUnits(i, squad);

                if (squad.HasAnyUnitsActive())
                    widget.gameObject.SetActiveSafe(true);
                else
                    widget.gameObject.SetActiveSafe(false);
            }

            _commanderComponent.OnCommandSquadUnitChanged += OnCommandUnitSquadChanged;
        }

        private void OnCommandUnitSquadChanged(int squadId, CommandSquad squad, int formationIndex)
        {
            UICommanderSquadWidget widget = _squadWidgets[squadId];

            widget.OnCommandSquadChanged(squadId, squad, formationIndex);

            if (squad.HasAnyUnitsActive())
            {
                widget.gameObject.SetActiveSafe(true);
            }
            else
            {
                widget.gameObject.SetActiveSafe(false);
            }

        }
    }
}
