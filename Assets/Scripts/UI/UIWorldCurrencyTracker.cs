using LichLord;
using LichLord.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class UIWorldCurrencyTracker : UIWidget
    {
        [SerializeField]
        private UIWorldCurrencySlot _slotPrefab;

        [SerializeField]
        private RectTransform _layoutGroup;

        private readonly Dictionary<CurrencyDefinition, UIWorldCurrencySlot> _currencySlots
            = new Dictionary<CurrencyDefinition, UIWorldCurrencySlot>();

        protected override void OnTick()
        {
            base.OnTick();

            var pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return;

            foreach (var currency in Context.ContainerManager.StockpileCurrencyTotals)
            {
                int total = currency.Value;
                // Add or remove slot based on total value
                if (total > 0)
                {
                    AddCurrencySlot(currency.Key);
                }
                else
                {
                    RemoveCurrencySlot(currency.Key);
                }
            }
        }

        private void RemoveCurrencySlot(CurrencyDefinition currencyType)
        {
            if (!_currencySlots.TryGetValue(currencyType, out var slot))
                return;

            Destroy(slot.gameObject);
            _currencySlots.Remove(currencyType);
        }

        private void AddCurrencySlot(CurrencyDefinition currencyType)
        {
            if (_currencySlots.ContainsKey(currencyType))
                return;

            var newSlot = Instantiate(_slotPrefab, _layoutGroup);
            newSlot.SetDefinition(currencyType);
            AddChild(newSlot);
            _currencySlots[currencyType] = newSlot;
        }
    }
}