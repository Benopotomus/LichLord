using LichLord.Props;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIFloatingNexusStatus : UIFloatingWidget
    {
        [Header("UI Elements")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TextMeshProUGUI _healthText;

        private Nexus _nexus;

        protected override void OnTick()
        {
            base.OnTick();

            PropRuntimeState nexusState = _nexus.RuntimeState;
            if (_nexus != null)
            {
                _iconImage.SetActive(true);
                _healthSlider.SetActive(true);
                _healthText.SetActive(true);
                _healthText.text = nexusState.GetHealth() + " / " + nexusState.GetMaxHealth();
                _healthSlider.value = (float)nexusState.GetHealth() / (float)nexusState.GetMaxHealth();
            }
            else
            {
                _iconImage.SetActive(false);
                _healthSlider.SetActive(false);
                _healthText.SetActive(false);
            }
        }

        public void SetNexus(Nexus nexus)
        {
            _nexus = nexus;
        }

    }
}
