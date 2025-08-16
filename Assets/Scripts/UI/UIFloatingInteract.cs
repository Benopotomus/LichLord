using JetBrains.Annotations;
using LichLord;
using LichLord.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Needed for Image

namespace LichLord.UI
{
    public class UIFloatingInteract : UIFloatingWidget
    {
        [Header("UI Elements")]
        [SerializeField] private Slider _progressSlider;  // Drag your progress bar fill image here

        [SerializeField] private TextMeshProUGUI _warningText;

        [SerializeField] private UIStockpileContents _stockpileContents;

        protected override void OnVisible()
        {
            base.OnVisible();
            _warningText.SetActive(false);
        }

        public override void SetTarget(Transform target)
        {
            if (target != _target)
                _warningText.SetActive(false);

            base.SetTarget(target);
        }

        public void SetProgressBarVisible(bool visible)
        {
            _progressSlider.SetActive(visible);
        }

        public void SetProgressBarPercent(float percent)
        {
            _progressSlider.value = Mathf.Clamp01(1 - percent);
        }

        public void ShowWarningMessage(string warningMessage)
        {
            _warningText.text = warningMessage;
            _warningText.SetActive(true);
            Invoke(nameof(HideWarningMessage), 3.0f);
        }

        private void HideWarningMessage()
        {
            _warningText.SetActive(false);
        }

        public void ShowStockpileContents(int stockpileIndex)
        {
            _stockpileContents.ShowStockpileContents(stockpileIndex);
        }
    }
}