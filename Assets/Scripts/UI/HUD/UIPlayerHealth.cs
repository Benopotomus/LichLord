using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace LichLord.UI
{
    public class UIPlayerHealth : UIWidget
    {
        [SerializeField]
        private Slider _healthSlider;

        protected override void OnTick()
        {
            if (Context.LocalPlayerCharacter == null)
                return;

            base.OnTick();

            _healthSlider.value = Context.LocalPlayerCharacter.Health.HealthPercent;

        }
    }
}
