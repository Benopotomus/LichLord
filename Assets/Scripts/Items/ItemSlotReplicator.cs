
using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Items
{
    public class ItemSlotReplicator : ContextBehaviour
    {
        [SerializeField]
        [Networked]
        public byte Index { get; set; }

        [Networked, Capacity(ContainerConstants.ITEMS_PER_REPLICATOR), OnChangedRender(nameof(OnItemSlotDataChanged))]
        protected virtual NetworkArray<FItemSlotData> _itemSlotDatas { get; }
        public NetworkArray<FItemSlotData> ItemSlotDatas => _itemSlotDatas;

        private FItemSlotData[] _localItemSlotDatas = new FItemSlotData[ContainerConstants.ITEMS_PER_REPLICATOR];

        public Action<int, FItemSlotData> OnItemSlotChanged;

        public override void Spawned()
        {
            base.Spawned();
            Context.ContainerManager.AddItemSlotReplicator(this);
            OnItemSlotDataChanged();
        }

        private void OnItemSlotDataChanged()
        {
            for (int i = 0; i < ContainerConstants.ITEMS_PER_REPLICATOR; i++)
            {
                var networkedSlot = ItemSlotDatas[i];
                if (!_localItemSlotDatas[i].IsEqual(networkedSlot))
                {
                    _localItemSlotDatas[i].Copy(networkedSlot);
                    OnItemSlotChanged?.Invoke(i + (Index * ContainerConstants.ITEMS_PER_REPLICATOR), networkedSlot);
                }
            }
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

        public void SetItemSlotData(int index, FItemSlotData itemSlotData)
        {
            _itemSlotDatas.Set(index, itemSlotData);
        }

        public void SetItemData(int index, FItemData itemData)
        {
            ref FItemSlotData itemSlotData = ref _itemSlotDatas.GetRef(index);
            itemSlotData.ItemData = itemData;
        }

        public void ClearItemData(int index)
        {
            ref FItemSlotData itemSlotData = ref _itemSlotDatas.GetRef(index);
            itemSlotData.IsAssigned = false;
            itemSlotData.ItemData = new FItemData();
        }

        public (int startIndex, int endIndex) GetItemSlotRange(int count)
        {
            if (count <= 0 || count > ContainerConstants.ITEMS_PER_REPLICATOR)
            {
                return (-1, -1); // Invalid count, return invalid range
            }

            int start = -1;
            int currentCount = 0;

            for (int i = 0; i < ContainerConstants.ITEMS_PER_REPLICATOR; i++)
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


