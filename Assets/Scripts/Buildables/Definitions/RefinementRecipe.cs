using LichLord.Items;
using System;
using System.Collections.Generic;

namespace LichLord
{
    [Serializable]
    public class RefinementRecipe
    {
        public List<FItemRecipeValue> InItems;
        public List<FItemRecipeValue> OutItems;

        public bool IsRecipeValid(List<(int, FItemSlotData)> itemDatas)
        {
            if (InItems == null || InItems.Count == 0)
                return false;

            if (itemDatas == null || itemDatas.Count == 0)
                return false;

            foreach (var required in InItems)
            {
                if (required.Item == null)
                    return false;

                int requiredDefId = required.Item.TableID;
                int requiredCount = required.Count;
                bool hasMatch = false;

                for (int i = 0; i < itemDatas.Count; i++)
                {
                    FItemData itemData = itemDatas[i].Item2.ItemData;
                    ItemDefinition itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID);
                    if (itemDefinition == null)
                        continue;

                    if (itemDefinition.TableID == requiredDefId)
                    {
                        int itemCount = itemDefinition.DataDefinition.GetStackCount(ref itemData);
                        if (itemCount >= requiredCount)
                        {
                            hasMatch = true;
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
            for (int i = 0; i < InItems.Count; i++)
            {
                if (itemDefinition == InItems[i].Item)
                    return InItems[i].Count;
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

                foreach (var recipeOut in OutItems)
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
            List<FItemData> completedItems = new List <FItemData>();

            foreach (var outItems in OutItems)
            {
                if (outItems.Item == null)
                    continue;

                FItemData completedItem = new FItemData();
                completedItem.DefinitionID = outItems.Item.TableID;
                outItems.Item.DataDefinition.SetStackCount (outItems.Count, ref completedItem);

                completedItems.Add (completedItem);
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
