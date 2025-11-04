using UnityEngine;
using System.Collections.Generic;
using LichLord.Buildables;
using LichLord.Items;

namespace LichLord.UI
{
    public class UIStockpileTooltip : UIWidget
    {
        [SerializeField] private RectTransform _layoutGroup;

        [SerializeField] private UIFloatingHealthbar _healthbar;

        [SerializeField] private UIStockpileCurrencySlot _stockpileCurrencyPrefab;

        private Dictionary<CurrencyDefinition, UIStockpileCurrencySlot> _stockpileSlots = 
            new Dictionary<CurrencyDefinition, UIStockpileCurrencySlot>();

        public void SetStockpileData(Stockpile stockpile)
        {
            int health = stockpile.RuntimeState.GetHealth();
            _healthbar.SetHealth(health, stockpile.RuntimeState.GetMaxHealth());

            int containerIndex = stockpile.RuntimeState.GetContainerIndex();
            bool isValidStockpile = containerIndex >= 0 && health > 0;

            // Create a set of currency types that are currently in the stockpile with non-zero values
            Dictionary<CurrencyDefinition, int> currencyAmounts = new Dictionary<CurrencyDefinition, int>();

            if (isValidStockpile)
            {
                // Get all item slots in this stockpile's container.
                var slotDatas = Context.ContainerManager.GetItemSlotDatasFromContainerIndex(containerIndex);

                // Sum currency stacks across all valid item slots.
                foreach (var slotData in slotDatas)
                {
                    if (!slotData.ItemData.IsValid())
                        continue;

                    var itemDef = Global.Tables.ItemTable.TryGetDefinition(slotData.ItemData.DefinitionID);
                    if (itemDef is CurrencyDefinition currencyDef)
                    {
                        FItemData tempData = slotData.ItemData;
                        var stackCount = itemDef.DataDefinition.GetStackCount(ref tempData);
                        if (stackCount > 0)
                        {
      
                            var currencyType = currencyDef.CurrencyType;
                            if (currencyAmounts.TryGetValue(currencyDef, out int currentTotal))
                            {
                                currencyAmounts[currencyDef] = currentTotal + stackCount;
                            }
                            else
                            {
                                currencyAmounts.Add(currencyDef, stackCount);
                            }
                        }
                    }
                }
            }

            // Create or update slots for non-zero currencies
            foreach (var currency in currencyAmounts)
            {
                CurrencyDefinition currencyDef = currency.Key;
                int value = currency.Value;

                if (_stockpileSlots.TryGetValue(currencyDef, out var existingSlot))
                {
                    // Update existing slot
                    existingSlot.UpdateValue(value);
                }
                else
                {
                    // Create new slot
                    UIStockpileCurrencySlot newSlot = Instantiate(_stockpileCurrencyPrefab, _layoutGroup);
                    newSlot.AssignContainerIndex(containerIndex);
                    newSlot.SetDefinition(currencyDef);
                    newSlot.UpdateValue(value);
                    _stockpileSlots[currencyDef] = newSlot;
                }
            }

            // Remove slots for currencies that are no longer in the stockpile
            List<CurrencyDefinition> currenciesToRemove = new List<CurrencyDefinition>();
            foreach (var slot in _stockpileSlots)
            {
                if (!currencyAmounts.ContainsKey(slot.Key))
                {
                    currenciesToRemove.Add(slot.Key);
                }
            }

            foreach (var currencyType in currenciesToRemove)
            {
                if (_stockpileSlots.TryGetValue(currencyType, out var slotToRemove))
                {
                    Destroy(slotToRemove.gameObject);
                }
                _stockpileSlots.Remove(currencyType);
            }
        }
    }
}