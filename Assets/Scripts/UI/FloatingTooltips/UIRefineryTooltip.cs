using LichLord.Buildables;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIRefineryTooltip : UIWidget
    {
        [SerializeField] private Refinery _refinery;
        [SerializeField] private UIFloatingHealthbar _healthbar;
        [SerializeField] private Slider _refineryProgressBar;

        public void SetRefinery(Refinery refinery)
        {
            _refinery = refinery;

            _healthbar.SetHealth(refinery.RuntimeState.GetHealth(), refinery.RuntimeState.GetMaxHealth());

            _refineryProgressBar.value = refinery.RefineryStateComponent.GetLocalRefineryProgress();
        }
    }
}