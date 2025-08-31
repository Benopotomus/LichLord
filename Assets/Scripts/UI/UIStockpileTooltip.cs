using TMPro;
using UnityEngine;
using System.Collections.Generic;
using LichLord.Buildables;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIStockpileTooltip : UIWidget
    {
        [SerializeField] private RectTransform _layoutGroup;

        [SerializeField] private UIStockpileCurrencySlot _stockpileCurrencyPrefab;

        private Dictionary<ECurrencyType, UIStockpileCurrencySlot> _stockpileSlots = new Dictionary<ECurrencyType, UIStockpileCurrencySlot>();

        public void SetStockpileData(Stockpile stockpile)
        {
            int stockpileIndex = stockpile.RuntimeState.GetStockpileIndex();
            // Get the stockpile data
            FStockpileData stockpileData = Context.ContainerManager.GetStockPile(stockpileIndex);

            // Create a set of currency types that are currently in the stockpile with non-zero values
            Dictionary<ECurrencyType, int> currencyAmounts = new Dictionary<ECurrencyType, int>();
            for (int i = 0; i < 4; i++)
            {
                FCurrencyStack stack = stockpileData.GetCurrencyStack(i);
                if (!stack.IsEmpty() && stack.Value > 0)
                {
                    if (currencyAmounts.ContainsKey(stack.CurrencyType))
                    {
                        currencyAmounts[stack.CurrencyType] += stack.Value;
                    }
                    else
                    {
                        currencyAmounts.Add(stack.CurrencyType, stack.Value);
                    }
                }
            }

            // Create or update slots for non-zero currencies
            foreach (var currency in currencyAmounts)
            {
                ECurrencyType currencyType = currency.Key;
                int value = currency.Value;

                if (_stockpileSlots.ContainsKey(currencyType))
                {
                    // Update existing slot
                    _stockpileSlots[currencyType].UpdateValue(value);
                }
                else
                {
                    // Create new slot
                    UIStockpileCurrencySlot newSlot = Instantiate(_stockpileCurrencyPrefab, _layoutGroup);
                    newSlot.AssignStockPileIndex(stockpileIndex);
                    newSlot.SetCurrencyType(currencyType);
                    newSlot.UpdateValue(value);
                    _stockpileSlots.Add(currencyType, newSlot);
                }
                _stockpileSlots[currencyType].gameObject.SetActive(stockpileIndex >= 0);
            }

            // Remove slots for currencies that are no longer in the stockpile
            List<ECurrencyType> currenciesToRemove = new List<ECurrencyType>();
            foreach (var slot in _stockpileSlots)
            {
                if (!currencyAmounts.ContainsKey(slot.Key))
                {
                    currenciesToRemove.Add(slot.Key);
                }
            }

            foreach (var currencyType in currenciesToRemove)
            {
                Destroy(_stockpileSlots[currencyType].gameObject);
                _stockpileSlots.Remove(currencyType);
            }
        }
    }
}