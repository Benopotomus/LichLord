using DWD.Utility.Loading;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIHeldCurrencySlot : UICurrencySlot
    {
        protected override void OnVisible()
        {
            base.OnVisible();
        }

        protected override void OnTick()
        {
            base.OnTick();

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            var currencyComponent = pc.Currency;
            var currencyType = _definition.CurrencyType;

            _text.text = currencyComponent.GetCurrencyCount(currencyType) + " / " + currencyComponent.GetCurrencyMax(currencyType);
        }
    }
}
