using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIFloatingHealthbar : UIWidget
    {
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TextMeshProUGUI _healthText;

        public void SetHealth(float currentHealth, float maxHealth)
        {
            _healthText.text = currentHealth + " / " + maxHealth;
            _healthSlider.value = currentHealth / (float)maxHealth;
        }
    }
}
