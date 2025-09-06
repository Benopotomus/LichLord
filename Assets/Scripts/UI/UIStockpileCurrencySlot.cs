using DWD.Utility.Loading;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIStockpileCurrencySlot : UICurrencySlot
    {
        private int _stockpileIndex;

        public void AssignStockPileIndex(int index)
        { 
            _stockpileIndex = index;
        }

        public void SetCurrencyType(ECurrencyType currencyType)
        {
            SetDefinition(Global.Tables.CurrencyTable.GetDefinition(currencyType));
        }

        public void UpdateValue(int value)
        {
            _text.text = value.ToString();
        }
    }
}
