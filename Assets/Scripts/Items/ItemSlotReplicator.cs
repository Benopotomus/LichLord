
using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Items
{
    public class ItemSlotReplicator : ContextBehaviour
    {
        [SerializeField]
        [Networked]
        public byte Index { get; set; }

        [Networked, Capacity(ItemConstants.ITEMS_PER_REPLICATOR)]
        protected virtual NetworkArray<FItemSlotData> _itemSlotDatas { get; }
        public NetworkArray<FItemSlotData> ItemSlotDatas => _itemSlotDatas;

        public override void Spawned()
        {
            base.Spawned();
            Context.ContainerManager.AddItemSlotReplicator(this);
        }

        public ref FItemSlotData GetItemSlotDataAtIndex(int index)
        { 
            return ref _itemSlotDatas.GetRef(index);
        }

        public List<FItemSlotData> GetItemsAtIndexRange(int start, int end)
        { 
            List<FItemSlotData> itemDatas = new List<FItemSlotData>();

            for (int i = start; i < end; i++) 
            { 
                itemDatas.Add(GetItemSlotDataAtIndex(i));
            }

            return itemDatas;
        
        }

        public void SetItemData(int index, FItemSlotData itemData)
        {
            _itemSlotDatas.Set(index, itemData);
        }

        public (int startIndex, int endIndex) GetItemSlotRange(int count)
        {
            if (count <= 0 || count > ItemConstants.ITEMS_PER_REPLICATOR)
            {
                return (-1, -1); // Invalid count, return invalid range
            }

            int start = -1;
            int currentCount = 0;

            for (int i = 0; i < ItemConstants.ITEMS_PER_REPLICATOR; i++)
            {
                if (!_itemSlotDatas[i].IsAssigned)
                {
                    if (start == -1)
                    {
                        start = i; // Start of a potential range
                    }
                    currentCount++;

                    if (currentCount == count)
                    {
                        return (start, start + count - 1); // Found a valid range (inclusive endIndex)
                    }
                }
                else
                {
                    // Reset if we hit an assigned slot
                    start = -1;
                    currentCount = 0;
                }
            }

            return (-1, -1); // No valid range found
        }

        public void SetIndexesAssigned(int startIndex, int endIndex, bool isAssigned)
        { 
            for(int i = startIndex; i <= endIndex; i++)
            _itemSlotDatas.GetRef(i).IsAssigned = isAssigned;
        }
    }
}


