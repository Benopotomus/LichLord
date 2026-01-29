
using UnityEngine;

namespace LichLord.UI
{
    public class UICommanderSquadTooltipTracker : UIWidget
    {
        [SerializeField]
        protected UICommanderSquadFloatingTooltip[] _tooltips;

        protected override void OnVisible()
        {
            base.OnVisible();

            Debug.Log("Visible");

            for (int i = 0; i < _tooltips.Length; i++)
                _tooltips[i].OnTrackerVisible(i);
        }

        public void LateUpdate()
        {
            for (int i = 0; i < _tooltips.Length; i++)
                _tooltips[i].OnTrackerLateUpdate();
        }
    }
}
