using LichLord;
using LichLord.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class UIHeldCurrencyTracker : UIWidget
    {
        [SerializeField]
        private UIHeldCurrencySlot _slotPrefab;

        [SerializeField]
        private RectTransform _layoutGroup;

        private readonly Dictionary<ECurrencyType, UIHeldCurrencySlot> _currencySlots
            = new Dictionary<ECurrencyType, UIHeldCurrencySlot>();

        protected override void OnTick()
        {
            base.OnTick();

            var pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return;

            // Loop through the fixed stacks in PlayerCurrencyComponent
            for (int i = 0; i < pc.Currency.CurrencyCount; i++)
            {
                var stack = pc.Currency.GetStackAtIndex(i);

                if (stack.Value > 0)
                {
                    AddCurrencySlot(stack.CurrencyType);
                }
                else
                {
                    RemoveCurrencySlot(stack.CurrencyType);
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
