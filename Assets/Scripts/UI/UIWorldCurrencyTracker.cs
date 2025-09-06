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

        private readonly Dictionary<ECurrencyType, UIWorldCurrencySlot> _currencySlots
            = new Dictionary<ECurrencyType, UIWorldCurrencySlot>();

        protected override void OnTick()
        {
            base.OnTick();

            var pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return;

            // Get all possible currency types from the player's currency component
            for (int i = 0; i < pc.Currency.CurrencyCount; i++)
            {
                var stack = pc.Currency.GetStackAtIndex(i);
                var currencyType = stack.CurrencyType;

                // Calculate total value (player + world)
                int playerValue = stack.Value;
                int totalValue = playerValue;
                if (Context.ContainerManager.AllCurrencies.TryGetValue(currencyType, out int worldValue))
                {
                    totalValue += worldValue;
                }

                // Add or remove slot based on total value
                if (totalValue > 0)
                {
                    AddCurrencySlot(currencyType);
                }
                else
                {
                    RemoveCurrencySlot(currencyType);
                }
            }
        }

        private void RemoveCurrencySlot(ECurrencyType currencyType)
        {
            if (!_currencySlots.TryGetValue(currencyType, out var slot))
                return;

            Destroy(slot.gameObject);
            _currencySlots.Remove(currencyType);
        }

        private void AddCurrencySlot(ECurrencyType currencyType)
        {
            if (_currencySlots.ContainsKey(currencyType))
                return;

            var newSlot = Instantiate(_slotPrefab, _layoutGroup);
            newSlot.SetDefinition(Context.LocalPlayerCharacter.Currency.GetCurrencyDefinition(currencyType));
            AddChild(newSlot);
            _currencySlots[currencyType] = newSlot;
        }
    }
}