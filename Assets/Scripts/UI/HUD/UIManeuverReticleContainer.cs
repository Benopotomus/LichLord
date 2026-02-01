using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIManeuverReticleContainer : UIWidget
    {
        [SerializeField] protected Image _borderLeft;
        [SerializeField] private TextMeshProUGUI _fireText;
        [SerializeField] protected Image _borderRight;
        [SerializeField] private TextMeshProUGUI _altFireText;

        [SerializeField] protected GameObject _progressBar;
        [SerializeField] protected TextMeshProUGUI _fillProgressText;
        [SerializeField] protected Slider _fillProgressSlider;

        public void OnSelectedManeuverChanged(ManeuverDefinition definition)
        {
            if (definition == null)
            {
                _fireText.text = _altFireText.text = "";
                _borderLeft.enabled = _borderRight.enabled = false;
                return;
            }

            _fireText.text = definition.ActivationTooltipText;
            _borderLeft.enabled = !string.IsNullOrEmpty(definition.ActivationTooltipText);

            var alt = definition.AltFireManeuver;
            if (alt == null)
            {
                _altFireText.text = "";
                _borderRight.enabled = false;
                return;
            }

            _altFireText.text = alt.ActivationTooltipText;
            _borderRight.enabled = !string.IsNullOrEmpty(alt.ActivationTooltipText);
        }

        public void OnActiveManeuverChanged(ManeuverDefinition definition)
        {
            if (definition == null)
            {
                _progressBar.SetActive(false);
                return;
            }
        }

        public void OnActiveManeuverUpdated(ManeuverDefinition definition, int ticksSinceStart)
        {
            if (string.IsNullOrEmpty(definition.ProgressTooltipText))
            {
                _progressBar.SetActive(false);
                return;
            }

            _borderLeft.enabled = _borderRight.enabled = false;
            _fireText.text = _altFireText.text = "";

            _progressBar.SetActive(true);
            _fillProgressText.text = definition.ProgressTooltipText;
            _fillProgressSlider.value = (float)ticksSinceStart / (float)definition.MaxHeldTicks;
        }

    }
}
