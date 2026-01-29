
using System;
using System.Collections;
using UnityEngine;

namespace LichLord.UI
{
    public class UICommandIconsSectionWidget : UIWidget
    {
        [SerializeField]
        private UISquadCommandSlot[] _slotWidgets;

        private PlayerCharacter _pc;

        protected override void OnVisible()
        {
            base.OnVisible();

            StartCoroutine(BindPlayerCharacter());

            ReverseGridOrder();

            for (int i = 0; i < _slotWidgets.Length; i++)
                _slotWidgets[i].SetSlot(i);
        }

        private void ReverseGridOrder()
        {
            for (int i = 0; i < _slotWidgets.Length; i++)
            {
                _slotWidgets[i].transform.SetSiblingIndex(
                    _slotWidgets.Length - 1 - i
                );
            }
        }

        protected override void OnHidden()
        {
            if (_pc != null)
            {
                _pc.Commander.OnCommandSquadUnitChanged -= OnCommandUnitSquadChanged;
                _pc.Maneuvers.OnSelectedManeuverChanged -= OnSelectedManeuverChanged;
            }

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
                if (i >= _pc.Maneuvers.CommandManeuvers.Count)
                    continue;

                CommandSquad squad = _pc.Commander.Squads[i];
                ManeuverDefinition maneuver = _pc.Maneuvers.CommandManeuvers[i];
                widget.SetManeuver(maneuver);
                OnCommandUnitSquadChanged(i, squad, 0);
                OnSelectedManeuverChanged(_pc.Maneuvers.GetSelectedManeuver());
            }

            _pc.Commander.OnCommandSquadUnitChanged += OnCommandUnitSquadChanged;
            _pc.Maneuvers.OnSelectedManeuverChanged += OnSelectedManeuverChanged;
        }

        private void OnSelectedManeuverChanged(ManeuverDefinition definition)
        {
            for (int i = 0; i < _slotWidgets.Length; i++)
                _slotWidgets[i].OnSelectedManeuverChanged(definition);
        }

        private void OnCommandUnitSquadChanged(int squadId, CommandSquad squad, int formationIndex)
        {
            UISquadCommandSlot widget = _slotWidgets[squadId];

            widget.OnCommandSquadChanged(squadId, squad, formationIndex);

        }


    }
}