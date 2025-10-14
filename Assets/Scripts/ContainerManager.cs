using Fusion;
using LichLord.Items;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace LichLord
{
    public class ContainerManager : ContextBehaviour
    {
        [SerializeField] private ItemSlotReplicator _itemSlotReplicatorPrefab;

        [SerializeField]
        private List<ItemSlotReplicator> _itemSlotReplicators = new List<ItemSlotReplicator>();
        public List<ItemSlotReplicator> ItemSlotReplicators => _itemSlotReplicators;

        [SerializeField] private ContainerReplicator _containerReplicatorPrefab;

        [SerializeField]
        private List<ContainerReplicator> _containerReplicators = new List<ContainerReplicator>();
        public List<ContainerReplicator> ContainerReplicators => _containerReplicators;

        public Action<int, FContainerSlotData> OnContainerSlotChanged;
        public Action<int, FItemSlotData> OnItemSlotChanged;

        private Dictionary<int, FPredictedItemData> _predictedItemSlots = new Dictionary<int, FPredictedItemData>();

        private Dictionary<CurrencyDefinition, int> _stockpileCurrencyTotals = new Dictionary<CurrencyDefinition, int>();
        public Dictionary<CurrencyDefinition, int> StockpileCurrencyTotals => _stockpileCurrencyTotals;

        // Containers

        public void LoadContainers()
        {
            var loadedContainers = Context.WorldSaveLoadManager.LoadedContainers;

            foreach (var containerSaveData in loadedContainers)
            {
                int replicatorIndex = containerSaveData.containerFullIndex / ContainerConstants.CONTAINERS_PER_REPLICATOR;
                int localIndex = containerSaveData.containerFullIndex % ContainerConstants.CONTAINERS_PER_REPLICATOR;

                var replicator = GetOrCreateContainerReplicatorForIndex(containerSaveData.containerFullIndex);
                ref FContainerSlotData data = ref replicator.GetContainerDataAtIndex(localIndex);
                data.StartIndex = containerSaveData.startIndex;
                data.EndIndex = containerSaveData.endIndex;
                data.IsAssigned = containerSaveData.isAssigned;
                data.IsStockpile = containerSaveData.isStockpile;
            }
        }

        public void SetupContainer(int slotCount, bool isStockpile = false)
        {
            var container = GetContainerFreeReplicatorAndIndex(slotCount);
            var itemSlots = GetItemReplicatorAndFreeSlotRange(slotCount);

            var fullItemIndexStart = itemSlots.startIndex + (itemSlots.replicator.Index * ContainerConstants.ITEMS_PER_REPLICATOR);
            var fullItemIndexEnd = itemSlots.endIndex + (itemSlots.replicator.Index * ContainerConstants.ITEMS_PER_REPLICATOR);

            container.replicator.AssignContainerIndex(container.freeIndex, fullItemIndexStart, fullItemIndexEnd, isStockpile);
            itemSlots.replicator.SetIndexesAssigned(itemSlots.startIndex, itemSlots.endIndex, true);
        }

        public void ClearContainer(int containerIndex)
        {
            // Get the container data for the given stockpile index
            FContainerSlotData containerData = GetContainerDataAtIndex(containerIndex);

            if (!containerData.IsAssigned)
            {
                Debug.LogWarning($"Container at index {containerIndex} is not assigned, cannot clear.");
                return;
            }

            // Calculate replicator and local index for the container
            int replicatorIndex = containerIndex / ContainerConstants.CONTAINERS_PER_REPLICATOR;
            int localIndex = containerIndex % ContainerConstants.CONTAINERS_PER_REPLICATOR;

            if (replicatorIndex >= _containerReplicators.Count)
            {
                Debug.LogError($"No container replicator found for index {containerIndex}");
                return;
            }

            // Get the container replicator
            var containerReplicator = _containerReplicators[replicatorIndex];

            // Unassign the container
            containerReplicator.ClearContainer(localIndex);

            // Get the item slot range for the container
            int startItemIndex = containerData.StartIndex;
            int endItemIndex = containerData.EndIndex;

            // Calculate the item slot replicator index
            int itemReplicatorIndex = startItemIndex / ContainerConstants.ITEMS_PER_REPLICATOR;

            if (itemReplicatorIndex >= _itemSlotReplicators.Count)
            {
                Debug.LogError($"No item slot replicator found for start index {startItemIndex}");
                return;
            }

            // Get the item slot replicator
            var itemSlotReplicator = _itemSlotReplicators[itemReplicatorIndex];

            // Unassign all item slots in the range
            itemSlotReplicator.SetIndexesAssigned(startItemIndex % ContainerConstants.ITEMS_PER_REPLICATOR,
                                                endItemIndex % ContainerConstants.ITEMS_PER_REPLICATOR,
                                                false);

            // Clear item data for each slot in the range
            for (int fullItemIndex = startItemIndex; fullItemIndex <= endItemIndex; fullItemIndex++)
            {
                int localItemIndex = fullItemIndex % ContainerConstants.ITEMS_PER_REPLICATOR;
                itemSlotReplicator.ClearItemData(localItemIndex);
            }
        }

        public (ContainerReplicator replicator, int freeIndex)
            GetContainerFreeReplicatorAndIndex(int count)
        {
            foreach (var replicator in _containerReplicators)
            {
                int freeIndex = replicator.GetFreeContainerIndex();
                if (freeIndex > -1)
                    return (replicator, freeIndex);
            }

            var newReplicator = SpawnContainerReplicator();

            if (newReplicator != null)
                return (newReplicator, 0);

            Debug.Log("No replicator with free slots found");
            return (null, -1);
        }

        public ContainerReplicator SpawnContainerReplicator()
        {
            if (!HasStateAuthority)
                return null;

            var newReplicator = Runner.Spawn(_containerReplicatorPrefab, Vector3.zero, Quaternion.identity, null,
                                onBeforeSpawned: (runner, obj) =>
                                {
                                    var r = obj.GetComponent<ContainerReplicator>();
                                    r.Index = (byte)_containerReplicators.Count;
                                }
            );

            AddContainerReplicator(newReplicator);

            return newReplicator;
        }

        public ContainerReplicator GetOrCreateContainerReplicatorForIndex(int fullIndex)
        {
            int replicatorIndex = fullIndex / ContainerConstants.CONTAINERS_PER_REPLICATOR;
            int localIndex = fullIndex % ContainerConstants.CONTAINERS_PER_REPLICATOR;

            foreach (var replicator in _containerReplicators)
            {
                if (replicator.Index == replicatorIndex)
                    return replicator;
            }

            var newReplicator = SpawnContainerReplicator();

            return newReplicator;
        }

        public void AddContainerReplicator(ContainerReplicator replicator)
        {
            if (!_containerReplicators.Contains(replicator))
            {
                _containerReplicators.Add(replicator);
                // Sort the list by Index
                _containerReplicators.Sort((a, b) => a.Index.CompareTo(b.Index));
                replicator.OnContainerSlotChanged += OnContainerReplictorSlotChanged;
                UpdateStockpileCurrencyTotals(); // Initial update after loading.
            }
        }

        private void OnContainerReplictorSlotChanged(int fullIndex, FContainerSlotData containerSlotData)
        {
            OnContainerSlotChanged?.Invoke(fullIndex, containerSlotData);
        }

        public FContainerSlotData GetContainerDataAtIndex(int fullIndex)
        {
            int localIndex = fullIndex % ContainerConstants.CONTAINERS_PER_REPLICATOR;
            int replicatorIndex = fullIndex / ContainerConstants.CONTAINERS_PER_REPLICATOR;

            if (_containerReplicators.Count <= replicatorIndex)
                return new FContainerSlotData();

            return _containerReplicators[replicatorIndex].GetContainerDataAtIndex(localIndex);
        }

        // Item Slots

        public void LoadItemSlots()
        {
            var loadedItemSlots = Context.WorldSaveLoadManager.LoadedItemSlots;

            foreach (var itemSlotSaveData in loadedItemSlots)
            {
                int replicatorIndex = itemSlotSaveData.fullItemSlotIndex / ContainerConstants.ITEMS_PER_REPLICATOR;
                int localIndex = itemSlotSaveData.fullItemSlotIndex % ContainerConstants.ITEMS_PER_REPLICATOR;

                var replicator = GetOrCreateItemSlotReplicatorForIndex(itemSlotSaveData.fullItemSlotIndex);
                ref FItemSlotData data = ref replicator.GetItemSlotDataAtIndex(localIndex);
                data.ItemData.DefinitionID = itemSlotSaveData.definitionId;
                data.ItemData.Data = itemSlotSaveData.data;
                data.IsAssigned = itemSlotSaveData.isAssigned;
            }
        }

        public ItemSlotReplicator GetOrCreateItemSlotReplicatorForIndex(int fullIndex)
        {
            int replicatorIndex = fullIndex / ContainerConstants.ITEMS_PER_REPLICATOR;
            int localIndex = fullIndex % ContainerConstants.ITEMS_PER_REPLICATOR;

            foreach (var replicator in _itemSlotReplicators)
            {
                if (replicator.Index == replicatorIndex)
                    return replicator;
            }

            var newReplicator = SpawnItemSlotReplicator();

            return newReplicator;
        }

        public ItemSlotReplicator SpawnItemSlotReplicator()
        {
            if (!HasStateAuthority)
                return null;

            var newReplicator = Runner.Spawn(_itemSlotReplicatorPrefab, Vector3.zero, Quaternion.identity, null,
                                onBeforeSpawned: (runner, obj) =>
                                {
                                    var r = obj.GetComponent<ItemSlotReplicator>();
                                    r.Index = (byte)_itemSlotReplicators.Count;
                                }
            );

            AddItemSlotReplicator(newReplicator);

            return newReplicator;
        }

        public void AddItemSlotReplicator(ItemSlotReplicator replicator)
        {
            if (!_itemSlotReplicators.Contains(replicator))
            {
                _itemSlotReplicators.Add(replicator);
                // Sort the list by Index
                _itemSlotReplicators.Sort((a, b) => a.Index.CompareTo(b.Index));
                replicator.OnItemSlotChanged += OnItemReplictorSlotChanged;
            }
        }

        private void OnItemReplictorSlotChanged(int fullIndex, FItemSlotData itemSlotData)
        {
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
            {
                if (_predictedItemSlots.TryGetValue(fullIndex, out FPredictedItemData predictedData))
                {
                    OnItemSlotChanged?.Invoke(fullIndex, predictedData.ItemSlotData);
                    return;
                }
            }

            OnItemSlotChanged?.Invoke(fullIndex, itemSlotData);

            // Check if this slot change affects a stockpile and update totals if so.
            if (IsItemSlotInStockpile(fullIndex))
            {
                UpdateStockpileCurrencyTotals();
            }
        }

        public (ItemSlotReplicator replicator, int startIndex, int endIndex) GetItemReplicatorAndFreeSlotRange(int count)
        {
            foreach (var replicator in _itemSlotReplicators)
            {
                var itemSlotRange = replicator.GetItemSlotRange(count);
                if (itemSlotRange.startIndex > -1)
                    return (replicator, itemSlotRange.startIndex, itemSlotRange.endIndex);
            }

            var newReplicator = SpawnItemSlotReplicator();
            if (newReplicator != null)
            {
                var itemSlotRange = newReplicator.GetItemSlotRange(count);

                return (newReplicator, itemSlotRange.startIndex, itemSlotRange.endIndex);
            }


            Debug.Log("No replicator with free slots found");
            return (null, -1, -1);
        }

        public List<FItemSlotData> GetItemSlotDatasFromContainerIndex(int containerFullIndex)
        {
            List<FItemSlotData> itemSlotDatas = new List<FItemSlotData>();

            // Get the container data for the given container index
            FContainerSlotData containerData = GetContainerDataAtIndex(containerFullIndex);

            if (!containerData.IsAssigned)
            {
                Debug.LogWarning($"Container at index {containerFullIndex} is not assigned.");
                return itemSlotDatas; // Return empty list if container is not assigned
            }

            // Calculate the range of item slots assigned to this container
            int startIndex = containerData.StartIndex;
            int endIndex = containerData.EndIndex;

            int replicatorIndex = startIndex / ContainerConstants.ITEMS_PER_REPLICATOR;

            if (_itemSlotReplicators.Count <= replicatorIndex)
            {
                Debug.LogWarning("Trying to get a replicator but not right index. " + startIndex);
                return itemSlotDatas;
            }

            var itemSlotReplicator = _itemSlotReplicators[replicatorIndex];

            // Iterate through the item slot range
            for (int fullItemIndex = startIndex; fullItemIndex <= endIndex; fullItemIndex++)
            {
                // Get the item slot data at the local index
                FItemSlotData itemSlotData = GetItemSlotData(fullItemIndex);
                itemSlotDatas.Add(itemSlotData);
            }

            return itemSlotDatas;
        }

        public FItemSlotData GetItemSlotData(int fullIndex)
        {
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
            {
                if (_predictedItemSlots.TryGetValue(fullIndex, out FPredictedItemData predictedData))
                {
                    return predictedData.ItemSlotData;
                }
            }

            int replicatorIndex = fullIndex / ContainerConstants.ITEMS_PER_REPLICATOR;
            int localIndex = fullIndex % ContainerConstants.ITEMS_PER_REPLICATOR;

            if (_itemSlotReplicators.Count <= replicatorIndex)
                return new FItemSlotData();

            return _itemSlotReplicators[replicatorIndex].GetItemSlotDataAtIndex(localIndex);
        }

        public void SetItemSlotData(int fullIndex, FItemData itemData)
        {
            int replicatorIndex = fullIndex / ContainerConstants.ITEMS_PER_REPLICATOR;
            int localIndex = fullIndex % ContainerConstants.ITEMS_PER_REPLICATOR;

            if (_itemSlotReplicators.Count <= replicatorIndex)
                return;

            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                _predictedItemSlots[fullIndex] = new FPredictedItemData
                {
                    EndTick = Runner.Tick + 32,
                    ItemSlotData = new FItemSlotData
                    {
                        ItemData = itemData
                    }
                };

            _itemSlotReplicators[replicatorIndex].SetItemData(localIndex, itemData);
        }

        public void AddItemToSlot(int fullIndex, FItemData addedItemData)
        {
            int replicatorIndex = fullIndex / ContainerConstants.ITEMS_PER_REPLICATOR;
            int localIndex = fullIndex % ContainerConstants.ITEMS_PER_REPLICATOR;

            if (_itemSlotReplicators.Count <= replicatorIndex)
                return;

            FItemSlotData itemAtSlot = GetItemSlotData(fullIndex);

            if (itemAtSlot.ItemData.DefinitionID == addedItemData.DefinitionID)
            {
                ItemDefinition itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemAtSlot.ItemData.DefinitionID);
                int stackCount = itemDefinition.DataDefinition.GetStackCount(ref itemAtSlot.ItemData);
                int stacksToAdd = itemDefinition.DataDefinition.GetStackCount(ref addedItemData);

                itemDefinition.DataDefinition.SetStackCount(stackCount + stacksToAdd, ref addedItemData);
            }

            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                _predictedItemSlots[fullIndex] = new FPredictedItemData
                {
                    EndTick = Runner.Tick + 32,
                    ItemSlotData = new FItemSlotData
                    {
                        ItemData = addedItemData
                    }
                };

            _itemSlotReplicators[replicatorIndex].SetItemData(localIndex, addedItemData);
        }

        public void RemoveItemStacksFromSlot(int fullIndex, int stacksToRemove)
        {
            int replicatorIndex = fullIndex / ContainerConstants.ITEMS_PER_REPLICATOR;
            int localIndex = fullIndex % ContainerConstants.ITEMS_PER_REPLICATOR;

            if (_itemSlotReplicators.Count <= replicatorIndex)
                return;

            // Get current item at the slot
            FItemSlotData itemAtSlot = GetItemSlotData(fullIndex);
            FItemData updatedItemData = itemAtSlot.ItemData;

            if (!updatedItemData.IsValid())
                return; // nothing to remove

            ItemDefinition itemDefinition = Global.Tables.ItemTable.TryGetDefinition(updatedItemData.DefinitionID);
            if (itemDefinition == null)
                return;

            int currentCount = itemDefinition.DataDefinition.GetStackCount(ref updatedItemData);
            int newCount = currentCount - stacksToRemove;

            if (newCount > 0)
            {
                itemDefinition.DataDefinition.SetStackCount(newCount, ref updatedItemData);
            }
            else
            {
                // stack depleted → clear the slot
                updatedItemData = default;
            }

            // Predictive update for clients
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
            {
                _predictedItemSlots[fullIndex] = new FPredictedItemData
                {
                    EndTick = Runner.Tick + 32,
                    ItemSlotData = new FItemSlotData
                    {
                        ItemData = updatedItemData
                    }
                };
            }

            // Commit to the actual replicator
            _itemSlotReplicators[replicatorIndex].SetItemData(localIndex, updatedItemData);
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SetItemSlotData(int fullIndex, FItemData itemData)
        {
            SetItemSlotData(fullIndex, itemData);
        }

        public override void Render()
        {
            base.Render();

            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
            {
                int tick = Runner.Tick;

                var keysToRemove = new List<int>();
                foreach (var predictedSlot in _predictedItemSlots)
                {
                    int index = predictedSlot.Key;
                    if (tick >= predictedSlot.Value.EndTick)
                    {
                        OnItemSlotChanged?.Invoke(index, GetItemSlotData(index));
                        keysToRemove.Add(index);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _predictedItemSlots.Remove(key);
                }
            }
        }

        // Check all indexes in container for stack and fit. return true if any can
        public bool CanStackAndFitContainer(int containerIndex, FItemData otherItem)
        {
            if (!otherItem.IsValid())
                return false; // Nothing to stack.

            var containerSlotData = GetContainerDataAtIndex(containerIndex);
            if (containerSlotData.StartIndex > containerSlotData.EndIndex)
                return false; // Invalid container.

            // Check each slot in the container range.
            for (int fullIndex = containerSlotData.StartIndex; fullIndex <= containerSlotData.EndIndex; fullIndex++)
            {
                if (CanStackAndFit(fullIndex, otherItem))
                    return true; // Any slot that fits works.
            }

            return false; // No slots can stack/fit.
        }

        public bool CanStackAndFit(int fullIndex, FItemData otherItem)
        {
            var itemAtSlot = GetItemSlotData(fullIndex).ItemData;
            if (!itemAtSlot.IsValid())
                return true; // Empty slot can always accept.

            if (!CanStack(fullIndex, otherItem))
                return false;

            var itemAtSlotDef = Global.Tables.ItemTable.TryGetDefinition(itemAtSlot.DefinitionID);
            var otherItemDef = Global.Tables.ItemTable.TryGetDefinition(otherItem.DefinitionID);

            // General stack check: Can we fit the other item on top without exceeding max?
            var currentStackCount = itemAtSlotDef.DataDefinition.GetStackCount(ref itemAtSlot);
            var otherStackCount = otherItemDef.DataDefinition.GetStackCount(ref otherItem); // Fixed ref param.
            var maxStackCount = itemAtSlotDef.MaxStackCount; // Assuming this is a int property.

            return (currentStackCount + otherStackCount) <= maxStackCount;
        }

        public bool CanStack(int fullIndex, FItemData otherItem)
        {
            var itemAtSlot = GetItemSlotData(fullIndex).ItemData;
            if (!itemAtSlot.IsValid())
                return true; // Empty slot can always accept.

            var itemAtSlotDef = Global.Tables.ItemTable.TryGetDefinition(itemAtSlot.DefinitionID);
            var otherItemDef = Global.Tables.ItemTable.TryGetDefinition(otherItem.DefinitionID);

            if (itemAtSlotDef != otherItemDef)
                return false; // Definitions must match exactly.

            return true;
        }

        public FItemData StackItem(int fullIndex, FItemData otherItem)
        {
            if (!otherItem.IsValid())
                return otherItem; // Nothing to stack.

            var itemAtSlot = GetItemSlotData(fullIndex).ItemData;
            var isSlotEmpty = !itemAtSlot.IsValid();

            var otherItemDef = Global.Tables.ItemTable.TryGetDefinition(otherItem.DefinitionID);
            if (otherItemDef == null)
                return otherItem; // Invalid other item.

            // If slot is empty, just place the whole item there (no stacking needed).
            if (isSlotEmpty)
            {
                SetItemSlotData(fullIndex, otherItem); // Assuming you have a setter method.
                return default; // Or new FItemData() for invalid/no excess.
            }

            var itemAtSlotDef = Global.Tables.ItemTable.TryGetDefinition(itemAtSlot.DefinitionID);
            if (itemAtSlotDef == null || itemAtSlotDef != otherItemDef)
                return otherItem; // Can't stack: return unchanged.

            // Special currency type check.
            if (itemAtSlotDef is CurrencyDefinition slotCurrencyDef &&
                otherItemDef is CurrencyDefinition otherCurrencyDef)
            {
                if (slotCurrencyDef.CurrencyType != otherCurrencyDef.CurrencyType)
                    return otherItem;
            }

            // Proceed with stacking.
            var currentStackCount = itemAtSlotDef.DataDefinition.GetStackCount(ref itemAtSlot);
            var otherStackCount = otherItemDef.DataDefinition.GetStackCount(ref otherItem);
            var maxStackCount = itemAtSlotDef.MaxStackCount;
            var newTotal = currentStackCount + otherStackCount;

            FItemData excessItem = default;
            if (newTotal > maxStackCount)
            {
                // Stack up to max, create excess.
                itemAtSlotDef.DataDefinition.SetStackCount(maxStackCount, ref itemAtSlot);
                excessItem = otherItem; // Copy base.
                otherItemDef.DataDefinition.SetStackCount(newTotal - maxStackCount, ref excessItem);
            }
            else
            {
                // Fully stack.
                itemAtSlotDef.DataDefinition.SetStackCount(newTotal, ref itemAtSlot);
            }

            // Update the slot with the (possibly updated) itemAtSlot.
            SetItemSlotData(fullIndex, itemAtSlot); // Assuming setter exists.

            return excessItem; // Invalid if no excess.
        }

        public List<int> GetAllStockpileContainers()
        {
            List<int> stockpileIndices = new List<int>();

            foreach (var replicator in _containerReplicators)
            {
                for (int localIndex = 0; localIndex < ContainerConstants.CONTAINERS_PER_REPLICATOR; localIndex++)
                {
                    var data = replicator.GetContainerDataAtIndex(localIndex);
                    if (data.IsAssigned && data.IsStockpile)
                    {
                        int fullIndex = replicator.Index * ContainerConstants.CONTAINERS_PER_REPLICATOR + localIndex;
                        stockpileIndices.Add(fullIndex);
                    }
                }
            }

            return stockpileIndices;
        }

        public List<FItemData> GetAllItemsInStockpiles()
        {
            List<FItemData> allItems = new List<FItemData>();

            var stockpileContainers = GetAllStockpileContainers();
            foreach (int containerIndex in stockpileContainers)
            {
                var slotDatas = GetItemSlotDatasFromContainerIndex(containerIndex);
                foreach (var slotData in slotDatas)
                {
                    if (slotData.ItemData.IsValid())
                    {
                        allItems.Add(slotData.ItemData);
                    }
                }
            }

            return allItems;
        }

        private void UpdateStockpileCurrencyTotals()
        {
            _stockpileCurrencyTotals.Clear();
            var allItems = GetAllItemsInStockpiles();
            foreach (var item in allItems)
            {
                var itemDef = Global.Tables.ItemTable.TryGetDefinition(item.DefinitionID);
                if (itemDef is CurrencyDefinition currencyDef)
                {
                    FItemData localItem = item; // Copy for ref compatibility in loop.
                    var stackCount = itemDef.DataDefinition.GetStackCount(ref localItem);
                    if (_stockpileCurrencyTotals.TryGetValue(currencyDef, out int currentTotal))
                    {
                        _stockpileCurrencyTotals[currencyDef] = currentTotal + stackCount;
                    }
                    else
                    {
                        _stockpileCurrencyTotals[currencyDef] = stackCount;
                    }
                }
            }
            _stockpileCurrencyTotals = new Dictionary<CurrencyDefinition, int>(_stockpileCurrencyTotals); // Shallow copy for immutability.
        }

        private bool IsItemSlotInStockpile(int fullItemIndex)
        {
            var stockpileContainers = GetAllStockpileContainers();
            foreach (int containerIndex in stockpileContainers)
            {
                var containerData = GetContainerDataAtIndex(containerIndex);
                if (fullItemIndex >= containerData.StartIndex && fullItemIndex <= containerData.EndIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public FItemData AddItemToContainer(int fullContainerIndex, FItemData item)
        {
            if (!item.IsValid())
            {
                Debug.LogWarning("Cannot add invalid item to container.");
                return item;
            }

            var containerData = GetContainerDataAtIndex(fullContainerIndex);
            if (!containerData.IsAssigned || containerData.StartIndex > containerData.EndIndex)
            {
                Debug.LogWarning($"Cannot add item to invalid container {fullContainerIndex}.");
                return item;
            }

            FItemData remainingItem = item;

            // Try stacking.
            for (int fullIndex = containerData.StartIndex; fullIndex <= containerData.EndIndex && remainingItem.IsValid(); fullIndex++)
            {
                // we ignore invalid item slots for this
                var itemAtSlot = GetItemSlotData(fullIndex).ItemData;
                if (!itemAtSlot.IsValid())
                    continue;

                if (CanStackAndFit(fullIndex, remainingItem))
                {
                    remainingItem = StackItem(fullIndex, remainingItem);
                    if (!remainingItem.IsValid())
                        return default; // fully placed
                }
            }

            //  Try empty slots.
            for (int fullIndex = containerData.StartIndex; fullIndex <= containerData.EndIndex && remainingItem.IsValid(); fullIndex++)
            {
                var itemAtSlot = GetItemSlotData(fullIndex).ItemData;
                if (!itemAtSlot.IsValid()) // Empty slot
                {
                    SetItemSlotData(fullIndex, remainingItem);
                    remainingItem = default;
                    break;
                }
            }

            if (remainingItem.IsValid())
            {
                Debug.LogWarning($"Could not fully place item in container {fullContainerIndex}; leftover: {remainingItem}");
            }

            return remainingItem;
        }

        public int GetEmptyItemIndex(int fullContainerIndex)
        {
            var containerData = GetContainerDataAtIndex(fullContainerIndex);
            if (!containerData.IsAssigned || containerData.StartIndex > containerData.EndIndex)
            {
                Debug.LogWarning($"Cannot add item to invalid container {fullContainerIndex}.");
                return -1;
            }

            //  Try empty slots.
            for (int fullIndex = containerData.StartIndex; fullIndex <= containerData.EndIndex; fullIndex++)
            {
                var itemAtSlot = GetItemSlotData(fullIndex).ItemData;
                if (!itemAtSlot.IsValid()) // Empty slot
                {
                    return fullIndex;
                }
            }

            return -1;
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_StackOrSwapItemAtSlot(byte playerIndex, ushort fullIndex, FItemData itemToStack)
        {
            if (!CanStack(fullIndex, itemToStack))
            {
                FItemData itemAtSlot = GetItemSlotData(fullIndex).ItemData;

                SetItemSlotData(fullIndex, itemToStack);

                if (itemAtSlot.IsValid())
                {
                    if (HasStateAuthority)
                        RPC_RefundItem(playerIndex, itemAtSlot);
                }

                return;
            }

            // On authority/local: perform the stack (excess discarded in RPC, but local caller gets it via public method).
            FItemData returned = StackItem(fullIndex, itemToStack);

            if (returned.IsValid())
            {
                if(HasStateAuthority) 
                    RPC_RefundItem(playerIndex, returned);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_RefundItem(byte playerIndex, FItemData returned)
        {
            var pc = Context.NetworkGame.GetPlayerByIndex(playerIndex);
            if (pc != null)
            {
                pc.Inventory.AddItemToInventory(returned);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_StackOrSwapItemsAtSlots(ushort fromSlot, ushort toSlot)
        {
            // Grab current items
            FItemData toItem = GetItemSlotData(toSlot).ItemData;
            FItemData fromItem = GetItemSlotData(fromSlot).ItemData;

            if (CanStack(toSlot, fromItem))
            {
                // StackItem returns any excess that couldn't fit (or invalid if fully stacked)
                FItemData excess = StackItem(toSlot, fromItem);
                SetItemSlotData(fromSlot, excess);
                return;
            }

            SetItemSlotData(toSlot, fromItem);

            if (toItem.IsValid())
                SetItemSlotData(toSlot, toItem);
            else
                SetItemSlotData(fromSlot, new FItemData());
        }
    }
}