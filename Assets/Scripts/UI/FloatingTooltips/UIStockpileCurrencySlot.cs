using DWD.Utility.Loading;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIStockpileCurrencySlot : UICurrencySlot
    {
        private int _containerIndex;

        public void AssignContainerIndex(int index)
        { 
            _containerIndex = index;
        }

        public void UpdateValue(int value)
        {
            _text.text = value.ToString();
        }
    }
}
