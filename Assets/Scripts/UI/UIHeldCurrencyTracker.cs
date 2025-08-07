using LichLord;
using LichLord.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class UIHeldCurrencyTracker : UIWidget
    {
        [SerializeField]
        private UIHeldCurrencySlot _slotPrefab;

        [SerializeField]
        private RectTransform _layoutGroup;

        private Dictionary<ECurrencyType, UIHeldCurrencySlot> _currencySlots = new Dictionary<ECurrencyType, UIHeldCurrencySlot>();

        protected override void OnTick()
        {
            base.OnTick();

            var pc = Context.LocalPlayerCharacter;

            if (pc != null)
            {

                if (pc.Currency.Wood > 0)
                    AddCurrencySlot(ECurrencyType.Wood);
                else
                    RemoveCurrencySlot(ECurrencyType.Wood);

                if (pc.Currency.Stone > 0)
                    AddCurrencySlot(ECurrencyType.Stone);
                else
                    RemoveCurrencySlot(ECurrencyType.Stone);

                if (pc.Currency.Iron > 0)
                    AddCurrencySlot(ECurrencyType.Iron);
                else
                    RemoveCurrencySlot(ECurrencyType.Iron);

                if (pc.Currency.Gold > 0)
                    AddCurrencySlot(ECurrencyType.Gold);
                else
                    RemoveCurrencySlot(ECurrencyType.Gold);

                if (pc.Currency.Souls > 0)
                    AddCurrencySlot(ECurrencyType.Souls);
                else
                    RemoveCurrencySlot(ECurrencyType.Souls);
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
            if (_currencySlots.TryGetValue(currencyType, out var slot))
                return;

            UIHeldCurrencySlot newSlot = Instantiate(_slotPrefab, _layoutGroup) as UIHeldCurrencySlot;
            newSlot.SetDefinition(Context.LocalPlayerCharacter.Currency.GetCurrencyDefinition(currencyType));
            AddChild(newSlot);
            _currencySlots[currencyType] = newSlot;

        }
    }
}
