using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIFloatingNexusHealth : UIWidget
    {
        [Header("UI Elements")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TextMeshProUGUI _healthText;

        protected override void OnTick()
        {
            base.OnTick();

            var nearestNexus = Context.NexusManager.GetNearestNexus(Context.LocalPlayerCharacter.transform.position);

            if (nearestNexus != null)
            {
                _iconImage.SetActive(true);
                _healthSlider.SetActive(true);
                _healthText.SetActive(true);
                _healthText.text = nearestNexus.GetHealth() + " / " + nearestNexus.GetMaxHealth();
                _healthSlider.value = (float)nearestNexus.GetHealth() / (float)nearestNexus.GetMaxHealth();
            }
            else
            {
                _iconImage.SetActive(false);
                _healthSlider.SetActive(false);
                _healthText.SetActive(false);
            }
        }

    }
}
