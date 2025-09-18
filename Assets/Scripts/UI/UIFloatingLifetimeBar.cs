using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIFloatingLifetimeBar : UIWidget
    {
        [SerializeField] private Slider _lifetimeSlider;

        public void SetLifetime(float currentLife, float maxLife)
        {
            _lifetimeSlider.value = (1f - (float)currentLife / (float)maxLife);
        }
    }
}
