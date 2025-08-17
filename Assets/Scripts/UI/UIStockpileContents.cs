using TMPro;
using UnityEngine;

namespace LichLord.UI
{
    public class UIStockpileContents : UIWidget
    {
        [SerializeField] private UIStockpileCurrencySlot[] _stockpileCurrencySlots;

        public void ShowStockpileContents(int stockpileIndex)
        {
            foreach (var slot in _stockpileCurrencySlots)
            { 
                slot.gameObject.SetActive(stockpileIndex >= 0);

                slot.AssignStockPileIndex(stockpileIndex);
            }
        }
    }
}
