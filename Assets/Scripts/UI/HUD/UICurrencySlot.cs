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
        protected CurrencyDefinition _definition;

        [SerializeField]
        protected TextMeshProUGUI _text;

        [SerializeField]
        protected Image _iconImage;

        protected IconLoader _iconLoader = new IconLoader();

        protected override void OnVisible()
        {
            base.OnVisible();
            LoadDefinition(_definition);
        }

        protected override void OnTick()
        {
            base.OnTick();
        }

        protected void LoadDefinition(CurrencyDefinition definition)
        {
            _definition = definition;
            LoadIcon(_definition.Icon);
        }

        public void SetDefinition(CurrencyDefinition definition)
        {
            _definition = definition;

            LoadDefinition(_definition);
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
