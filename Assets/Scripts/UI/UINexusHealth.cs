using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UINexusHealth : UIWidget
    {
        [Header("UI Elements")]
        [SerializeField] private Slider _healthSlider;

        protected override void OnTick()
        {
            base.OnTick();

            var nearestNexus = Context.LocalPlayerCharacter.Nexus.NearestNexus;

            if (nearestNexus != null)
            {
                _healthSlider.value = nearestNexus.GetHealth();
            }


        }

    }
}
