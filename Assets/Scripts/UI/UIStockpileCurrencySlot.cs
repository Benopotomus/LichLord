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

        protected override void OnTick()
        {
            base.OnTick();

            if (_stockpileIndex == -1)
            {
                return;
            }

            FStockpileData stockpileData = Context.ContainerManager.GetStockPile(_stockpileIndex);

            var currencyType = _definition.CurrencyType;
            var count = stockpileData.GetCurrencyAmount(currencyType);


            _text.text = count.ToString();
        }

        public void AssignStockPileIndex(int index)
        { 
            _stockpileIndex = index;
        }

    }
}
