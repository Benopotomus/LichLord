using Fusion;
using LichLord.Items;
using LichLord.World;
using Pathfinding.RVO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    public class ContainerManager : ContextBehaviour
    {
        private const int MAX_STOCKPILES = 128;

        [Networked, Capacity(MAX_STOCKPILES)]
        [OnChangedRender(nameof(OnRep_StockpileDatas))]
        protected virtual NetworkArray<FStockpileData> _stockpileDatas { get; }

        private FStockpileData[] _authorityStockpileDatas = new FStockpileData[MAX_STOCKPILES];
        Dictionary<int, FStockpileData> _predictedStockpileDatas = new Dictionary<int, FStockpileData>();

        [SerializeField] private ItemSlotReplicator _itemSlotReplicatorPrefab;

        [SerializeField]
        private List<ItemSlotReplicator> _itemSlotReplicators = new List<ItemSlotReplicator>();
        public List<ItemSlotReplicator> ItemSlotReplicators => _itemSlotReplicators;

        [SerializeField] private ContainerReplicator _containerReplicatorPrefab;

        [SerializeField]
        private List<ContainerReplicator> _containerReplicators = new List<ContainerReplicator>();
        public List<ContainerReplicator> ContainerReplicators => _containerReplicators;

        public Action<int, FItemSlotData> OnItemSlotChanged;

        // Containers

        public void LoadContainers()
        {
            var loadedContainers = Context.WorldSaveLoadManager.LoadedContainers;

            foreach (var containerSaveData in loadedContainers)
            {
                int replicatorIndex = containerSaveData.containerFullIndex / ItemConstants.CONTAINERS_PER_REPLICATOR;
                int localIndex = containerSaveData.containerFullIndex % ItemConstants.CONTAINERS_PER_REPLICATOR;

                var replicator = GetOrCreateContainerReplicatorForIndex(containerSaveData.containerFullIndex);
                ref FContainerSlotData data = ref replicator.GetContainerDataAtIndex(localIndex);
                data.StartIndex = containerSaveData.startIndex;
                data.EndIndex = containerSaveData.endIndex;
                data.IsAssigned = containerSaveData.isAssigned;
            }
        }

        public void SetupContainer(int slotCount)
        {
            var container = GetContainerFreeReplicatorAndIndex(slotCount);
            var itemSlots = GetItemReplicatorAndFreeSlotRange(slotCount);

            var fullItemIndexStart = itemSlots.startIndex + (itemSlots.replicator.Index * ItemConstants.ITEMS_PER_REPLICATOR);
            var fullItemIndexEnd = itemSlots.endIndex + (itemSlots.replicator.Index * ItemConstants.ITEMS_PER_REPLICATOR);

            container.replicator.AssignContainerIndex(container.freeIndex, fullItemIndexStart, fullItemIndexEnd);
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
            int replicatorIndex = containerIndex / ItemConstants.CONTAINERS_PER_REPLICATOR;
            int localIndex = containerIndex % ItemConstants.CONTAINERS_PER_REPLICATOR;

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
            int itemReplicatorIndex = startItemIndex / ItemConstants.ITEMS_PER_REPLICATOR;

            if (itemReplicatorIndex >= _itemSlotReplicators.Count)
            {
                Debug.LogError($"No item slot replicator found for start index {startItemIndex}");
                return;
            }

            // Get the item slot replicator
            var itemSlotReplicator = _itemSlotReplicators[itemReplicatorIndex];

            // Unassign all item slots in the range
            itemSlotReplicator.SetIndexesAssigned(startItemIndex % ItemConstants.ITEMS_PER_REPLICATOR,
                                                endItemIndex % ItemConstants.ITEMS_PER_REPLICATOR,
                                                false);

            // Clear item data for each slot in the range
            for (int fullItemIndex = startItemIndex; fullItemIndex <= endItemIndex; fullItemIndex++)
            {
                int localItemIndex = fullItemIndex % ItemConstants.ITEMS_PER_REPLICATOR;
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
            int replicatorIndex = fullIndex / ItemConstants.CONTAINERS_PER_REPLICATOR;
            int localIndex = fullIndex % ItemConstants.CONTAINERS_PER_REPLICATOR;

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
            }
        }

        public FContainerSlotData GetContainerDataAtIndex(int fullIndex)
        {
            int localIndex = fullIndex % ItemConstants.CONTAINERS_PER_REPLICATOR;
            int replicatorIndex = fullIndex / ItemConstants.CONTAINERS_PER_REPLICATOR;

            if (_containerReplicators.Count < replicatorIndex)
                return new FContainerSlotData();

            return _containerReplicators[replicatorIndex].GetContainerDataAtIndex(localIndex);
        }

        // Item Slots

        public void LoadItemSlots()
        {
            var loadedItemSlots = Context.WorldSaveLoadManager.LoadedItemSlots;

            foreach (var itemSlotSaveData in loadedItemSlots)
            {
                int replicatorIndex = itemSlotSaveData.fullItemSlotIndex / ItemConstants.ITEMS_PER_REPLICATOR;
                int localIndex = itemSlotSaveData.fullItemSlotIndex % ItemConstants.ITEMS_PER_REPLICATOR;

                var replicator = GetOrCreateItemSlotReplicatorForIndex(itemSlotSaveData.fullItemSlotIndex);
                ref FItemSlotData data = ref replicator.GetItemSlotDataAtIndex(localIndex);
                data.ItemData.DefinitionID = itemSlotSaveData.definitionId;
                data.ItemData.Data = itemSlotSaveData.data;
                data.IsAssigned = itemSlotSaveData.isAssigned;
            }
        }

        public ItemSlotReplicator GetOrCreateItemSlotReplicatorForIndex(int fullIndex)
        {
            int replicatorIndex = fullIndex / ItemConstants.ITEMS_PER_REPLICATOR;
            int localIndex = fullIndex % ItemConstants.ITEMS_PER_REPLICATOR;

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
                replicator.OnItemSlotChanged += (fullIndex, itemSlotData) => OnItemSlotChanged?.Invoke(fullIndex, itemSlotData);
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

            int replicatorIndex = startIndex / ItemConstants.ITEMS_PER_REPLICATOR;

            if (_itemSlotReplicators.Count < replicatorIndex)
            {
                Debug.LogWarning("Trying to get a replicator but not right index. " + startIndex);
                return itemSlotDatas;
            }

            var itemSlotReplicator = _itemSlotReplicators[replicatorIndex];

            // Iterate through the item slot range
            for (int fullItemIndex = startIndex; fullItemIndex <= endIndex; fullItemIndex++)
            {
                int localIndex = fullItemIndex % ItemConstants.ITEMS_PER_REPLICATOR;

                // Get the item slot data at the local index
                FItemSlotData itemSlotData = itemSlotReplicator.GetItemSlotDataAtIndex(localIndex);
                itemSlotDatas.Add(itemSlotData);
            }

            return itemSlotDatas;
        }

        public FItemSlotData GetItemSlotData(int fullIndex)
        {
            int replicatorIndex = fullIndex / ItemConstants.ITEMS_PER_REPLICATOR;
            int localIndex = fullIndex % ItemConstants.ITEMS_PER_REPLICATOR;

            if (_itemSlotReplicators.Count < replicatorIndex)
                return new FItemSlotData();

            return _itemSlotReplicators[replicatorIndex].GetItemSlotDataAtIndex(localIndex);
        }

        public void SetItemSlotData(int fullIndex, FItemData itemData)
        {
            int replicatorIndex = fullIndex / ItemConstants.ITEMS_PER_REPLICATOR;
            int localIndex = fullIndex % ItemConstants.ITEMS_PER_REPLICATOR;

            if (_itemSlotReplicators.Count < replicatorIndex)
                return;

            _itemSlotReplicators[replicatorIndex].SetItemData(localIndex, itemData);
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SetItemSlotData(int fullIndex, FItemData itemData)
        {
            SetItemSlotData(fullIndex, itemData);
            // ref FItemSlotData = GetItemSlotData(fullIndex);
        }

        // Stockpiles

        private void OnRep_StockpileDatas(NetworkBehaviourBuffer previous)
        {
            UpdateAllStockpiles();
        }

        public int StockpileCount => _stockpileDatas.Length;

        private Dictionary<ECurrencyType, int> _allCurrencies = new Dictionary<ECurrencyType, int>();
        public Dictionary<ECurrencyType, int> AllCurrencies => _allCurrencies;

        public override void Spawned()
        {
            base.Spawned();
            UpdateAllStockpiles();
        }

        public FStockpileData GetStockPile(int index)
        {
            if (_predictedStockpileDatas.TryGetValue(index, out var data))
                return data;

            return _stockpileDatas.GetRef(index);
        }

        public int GetFreeStockpileIndex()
        {
            for (int i = 0; i < MAX_STOCKPILES; i++)
            {
                ref FStockpileData stockpile = ref _stockpileDatas.GetRef(i);
                if (!stockpile.IsAssigned) // not taken
                {
                    return i;
                }
            }
            return -1; // no free index found
        }

        public void AssignStockpileIndex(int index)
        {
            ref FStockpileData stockpile = ref _stockpileDatas.GetRef(index);
            stockpile.Assign();
        }

        public void LoadStockPileData(FStockpileSaveData stockpileSave)
        {
            ref FStockpileData stockpileData = ref _stockpileDatas.GetRef(stockpileSave.index);
            stockpileData = stockpileSave.ToNetworkStockpile();
            _stockpileDatas.Set(stockpileSave.index, stockpileData);
        }

        public void ClearStockpile(int stockpileIndex)
        {
            ref FStockpileData stockpileData = ref _stockpileDatas.GetRef(stockpileIndex);
            stockpileData.ClearStockpile();
            stockpileData.Unassign();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_StockpileDropOff_Player(int stockpileIndex, ECurrencyType currencyType, int value, PlayerCharacter pc)
        {
            int returnValue = AddToStockpile(stockpileIndex, currencyType, value);

            if (returnValue > 0)
            {
                if (HasStateAuthority)
                {
                    pc.Currency.RPC_AddCurrency(currencyType, returnValue);
                }
            }
        }

        public int AddToStockpile(int stockpileIndex, ECurrencyType currencyType, int value)
        {
            ref FStockpileData stockpile = ref _stockpileDatas.GetRef(stockpileIndex);
            return stockpile.AddToStockpile(currencyType, value);
        }

        public void Predict_StockpileDropOff(int stockpileIndex, ECurrencyType currencyType, int value)
        {
            FStockpileData stockpile = _stockpileDatas.Get(stockpileIndex);
            int returnValue = stockpile.AddToStockpile(currencyType, value);
            _predictedStockpileDatas[stockpileIndex] = stockpile;
            UpdateAllStockpiles();
        }

        public void UpdateAllStockpiles()
        {
            _allCurrencies.Clear();

            // Step 1: Track which indices we already handled via predictions
            HashSet<int> handledIndices = new HashSet<int>();

            // Step 2: Collect predicted keys to iterate safely
            int[] predictedKeys = new int[_predictedStockpileDatas.Count];
            _predictedStockpileDatas.Keys.CopyTo(predictedKeys, 0);

            foreach (int index in predictedKeys)
            {
                FStockpileData predicted = _predictedStockpileDatas[index];
                ref FStockpileData networkStockpile = ref _stockpileDatas.GetRef(index);

                if (_authorityStockpileDatas[index].IsEqual(networkStockpile))
                {
                    // Network and authority match, prediction is valid
                    AddStockpileCurrencies(predicted);
                }
                else
                {
                    // Authority diverged — discard prediction
                    _predictedStockpileDatas.Remove(index);
                    AddStockpileCurrencies(networkStockpile);
                }

                // Update authoritative cache
                _authorityStockpileDatas[index].Copy(networkStockpile);
                handledIndices.Add(index);
            }

            // Step 3: Loop through remaining network stockpiles
            for (int i = 0; i < MAX_STOCKPILES; i++)
            {
                if (handledIndices.Contains(i))
                    continue; // Already processed via predicted stockpile

                ref FStockpileData stockpile = ref _stockpileDatas.GetRef(i);
                _authorityStockpileDatas[i].Copy(stockpile);

                AddStockpileCurrencies(stockpile);
            }

            /*
            // Optional: Debug log totals
            foreach (var kvp in _allCurrencies)
            {
                Debug.Log($"Currency: {kvp.Key}, Amount: {kvp.Value}");
            }
            */
        }

        // Helper to add totals
        private void AddStockpileCurrencies(FStockpileData stockpile)
        {
            if (stockpile.IsEmpty())
                return;

            foreach (ECurrencyType type in Enum.GetValues(typeof(ECurrencyType)))
            {
                int amount = stockpile.GetCurrencyAmount(type);
                if (amount > 0)
                {
                    if (_allCurrencies.ContainsKey(type))
                        _allCurrencies[type] += amount;
                    else
                        _allCurrencies[type] = amount;
                }
            }
        }

    }
}