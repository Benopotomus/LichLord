using DWD.Utility.Loading;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UICurrencySlot : UIWidget
    {
        [SerializeField]
        private CurrencyDefinition _definition;

        [SerializeField]
        private TextMeshProUGUI _text;

        [SerializeField]
        private Image _iconImage;

        private IconLoader _iconLoader = new IconLoader();

        protected override void OnVisible()
        {
            base.OnVisible();
            LoadDefinition(_definition);
        }

        protected override void OnTick()
        {
            base.OnTick();

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            _text.text = pc.Currency.Wood.ToString();

        }

        private void LoadDefinition(CurrencyDefinition definition)
        {
            _definition = definition;
            LoadIcon(_definition.Icon);
        }

        // VISUALS

        private void LoadIcon(BundleObject prefabBundle)
        {
            _iconLoader.OnLoaded += OnIconLoaded;
            _iconLoader.LoadIcon(prefabBundle);
        }

        private void OnIconLoaded(IconLoader iconLoader, Sprite sprite)
        {
            _iconLoader.OnLoaded -= OnIconLoaded;
            _iconImage.sprite = sprite;
        }
    }
}
