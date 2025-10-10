using LichLord.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    [Serializable]
    public class RefinementRecipe
    {
        [SerializeField]
        private int _arcaneLevelRequirement;

        [SerializeField]
        private int _ticksPerProgress = 32;
        public int TicksPerProgress => _ticksPerProgress;

        [SerializeField]
        private List<FItemRecipeValue> _inItems;
        public List<FItemRecipeValue> InItems => _inItems;

        [SerializeField]
        private List<FItemRecipeValue> _outItems;
        public List<FItemRecipeValue> OutItems => _outItems;

        public bool IsRecipeValid(List<(int, FItemSlotData)> itemDatas)
        {
            if (_inItems == null || _inItems.Count == 0)
                return false;

            if (itemDatas == null || itemDatas.Count < _inItems.Count)
                return false; // Not enough slots to match all required items

            // Create a list of available slots that can be assigned
            List<(int slotIndex, FItemSlotData slotData)> availableSlots = new List<(int, FItemSlotData)>(itemDatas);
            List<(int, FItemSlotData)> usedSlots = new List<(int, FItemSlotData)>();

            // Try to match each required item to a unique slot
            foreach (var required in _inItems)
            {
                if (required.Item == null)
                    return false;

                int requiredDefId = required.Item.TableID;
                int requiredCount = required.Count;
                bool hasMatch = false;

                for (int i = 0; i < availableSlots.Count; i++)
                {
                    var (slotIndex, slotData) = availableSlots[i];
                    FItemData itemData = slotData.ItemData;
                    if (!itemData.IsValid())
                        continue;

                    ItemDefinition itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID);
                    if (itemDefinition == null)
                        continue;

                    if (itemDefinition.TableID == requiredDefId)
                    {
                        int itemCount = itemDefinition.DataDefinition.GetStackCount(ref itemData);
                        if (itemCount >= requiredCount)
                        {
                            hasMatch = true;
                            usedSlots.Add((slotIndex, slotData));
                            availableSlots.RemoveAt(i); // Remove the slot so it can't be reused
                            break;
                        }
                    }
                }

                if (!hasMatch)
                    return false;
            }

            return true;
        }

        public int GetStacksToRemove(ItemDefinition itemDefinition)
        {
            foreach (var recipeItem in _inItems)
            {
                if (itemDefinition == recipeItem.Item)
                {
                    return recipeItem.Count;
                }
            }

            return 0;
        }

        public bool CanProgress(List<(int, FItemSlotData)> outSlots)
        {
            foreach (var (_, slot) in outSlots)
            {
                // Ignore empty output slots — these are fine to fill
                if (!slot.ItemData.IsValid())
                    continue;

                ItemDefinition slotDefinition = Global.Tables.ItemTable.TryGetDefinition(slot.ItemData.DefinitionID);
                if (slotDefinition == null)
                    return false;

                bool isPartOfRecipe = false;

                foreach (var recipeOut in _outItems)
                {
                    if (recipeOut.Item.TableID == slotDefinition.TableID)
                    {
                        FItemData tempItem = slot.ItemData;
                        int currentCount = slotDefinition.DataDefinition.GetStackCount(ref tempItem);
                        int maxStack = slotDefinition.MaxStackCount;

                        if (currentCount >= maxStack)
                            return false;

                        isPartOfRecipe = true;
                        break;
                    }
                }

                // If any item in the output slots doesn’t belong to this recipe, invalid
                if (!isPartOfRecipe)
                    return false;
            }

            return true;
        }

        public List<FItemData> GetCompletedItems()
        {
            List<FItemData> completedItems = new List<FItemData>();

            foreach (var outItem in _outItems)
            {
                if (outItem.Item == null)
                    continue;

                FItemData completedItem = new FItemData();
                completedItem.DefinitionID = outItem.Item.TableID;
                outItem.Item.DataDefinition.SetStackCount(outItem.Count, ref completedItem);

                completedItems.Add(completedItem);
            }

            return completedItems;
        }
    }

    [Serializable]
    public struct FItemRecipeValue
    {
        public ItemDefinition Item;
        public int Count;
    }
}