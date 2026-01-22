
using System.Collections;
using UnityEngine;

namespace LichLord.UI
{
    public class UICommanderSquadWidget : UIWidget
    {
        [SerializeField]
        private int _squadId;

        [SerializeField]
        private UICommanderUnitIcon[] _commanderUnitIcons;

        [SerializeField]
        private CommanderComponent _commanderComponent;

        protected override void OnVisible()
        {
            base.OnVisible();
            StartCoroutine(BindCommanderComponent());
        }

        protected override void OnHidden()
        {
            if(_commanderComponent != null)
                _commanderComponent.OnCommandSquadUnitChanged -= OnCommandSquadChanged;

            base.OnHidden();
        }

        private IEnumerator BindCommanderComponent()
        {
            if (Context.LocalPlayerCharacter == null)
                yield return null;

            _commanderComponent = Context.LocalPlayerCharacter.Commander;

            for (int i = 0; i < _commanderUnitIcons.Length; i++) 
            {
                OnCommandSquadChanged(_squadId, _commanderComponent.Squads[_squadId], i);
            }

            _commanderComponent.OnCommandSquadUnitChanged += OnCommandSquadChanged;
        }

        private void OnCommandSquadChanged(int squadId, CommandSquad squad, int formationIndex)
        {
            if (squadId != _squadId)
                return;

            FCommandUnit unit = squad.CommandUnits[formationIndex];

            _commanderUnitIcons[formationIndex].SetCommandUnit(unit);
        }
    }
}
