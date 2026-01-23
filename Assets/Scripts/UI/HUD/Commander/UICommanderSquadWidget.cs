using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UICommanderSquadWidget : UIWidget
    {
        [SerializeField]
        private TextMeshProUGUI _squadText;

        [SerializeField]
        private Image _backgroundUI;

        [SerializeField]
        private UICommanderUnitIcon[] _commanderUnitIcons;

        [SerializeField]
        private CommanderComponent _commanderComponent;

        public void OnCommandSquadChanged(int squadId, CommandSquad squad, int formationIndex)
        {
            FCommandUnit unit = squad.CommandUnits[formationIndex];

            _commanderUnitIcons[formationIndex].SetCommandUnit(unit);
        }

        public void UpdateAllUnits(int squadId, CommandSquad squad)
        {
            for (int i = 0; i < _commanderUnitIcons.Length; i++)
            {
                OnCommandSquadChanged(squadId, squad, i);
            }
        }

        protected override void OnVisible()
        {
            base.OnVisible();
            _squadText.enabled = true;
            _backgroundUI.enabled = true;
        }

        protected override void OnHidden()
        {
            _squadText.enabled = false;
            _backgroundUI.enabled = false;
            base.OnHidden();
        }
    }
}
