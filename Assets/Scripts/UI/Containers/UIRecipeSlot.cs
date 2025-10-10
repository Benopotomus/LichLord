using LichLord.Items;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{
    public class UIRecipeSlot : UIWidget
    {
        [SerializeField]
        private List<UIItemSlot> _inItemSlots = new();

        [SerializeField]
        private List<UIItemSlot> _outItemSlots = new();

        public void SetRecipe(RefinementRecipe recipe)
        {
            // --- Setup Input Slots ---
            for (int i = 0; i < _inItemSlots.Count; i++)
            {
                if (i < recipe.InItems.Count)
                {
                    ItemDefinition defintion = recipe.InItems[i].Item;
                    int stackCount = recipe.InItems[i].Count;
                    // Create temp item
                    FItemData tempItem = new FItemData();
                    tempItem.DefinitionID = defintion.TableID;
                    defintion.DataDefinition.SetStackCount(stackCount, ref tempItem);

                    _inItemSlots[i].SetItemData(tempItem);
                    _inItemSlots[i].SetActive(true);
                }
                else
                {
                    _inItemSlots[i].SetActive(false);
                }
            }

            // --- Setup Input Slots ---
            for (int i = 0; i < _outItemSlots.Count; i++)
            {
                if (i < recipe.OutItems.Count)
                {
                    ItemDefinition defintion = recipe.OutItems[i].Item;
                    int stackCount = recipe.OutItems[i].Count;
                    // Create temp item
                    FItemData tempItem = new FItemData();
                    tempItem.DefinitionID = defintion.TableID;
                    defintion.DataDefinition.SetStackCount(stackCount, ref tempItem);

                    _outItemSlots[i].SetItemData(tempItem);
                    _outItemSlots[i].SetActive(true);
                }
                else
                {
                    _outItemSlots[i].SetActive(false);
                }
            }

        }
    }
}
