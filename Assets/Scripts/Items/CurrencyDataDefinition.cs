
using UnityEngine;

namespace LichLord.Items
{
    [CreateAssetMenu(fileName = "CurrencyDataDefinition", menuName = "LichLord/Items/CurrencyDataDefinition")]
    public class CurrencyDataDefinition : ItemDataDefinition
    {
        protected const int STACK_COUNT_BITS = 10;         // 0-1023
        protected const int STACK_COUNT_SHIFT = 0;
        protected const int STACK_COUNT_MASK = (1 << STACK_COUNT_BITS) - 1;

        public override void InitializeData(ref FItemData itemData, ItemDefinition definition)
        {

        }

        // Stack Count
        public override int GetStackCount(ref FItemData itemData)
        {
            return (itemData.Data >> STACK_COUNT_SHIFT) & STACK_COUNT_MASK;
        }

        public override void SetStackCount(int index, ref FItemData itemData)
        {
            int data = itemData.Data;
            data = (data & ~(STACK_COUNT_MASK << STACK_COUNT_SHIFT)) | (index << STACK_COUNT_SHIFT);
            itemData.Data = data;
        }

    }


}