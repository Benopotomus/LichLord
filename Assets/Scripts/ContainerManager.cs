using Fusion;
using LichLord.Buildables;
using LichLord.World;
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
            if(_predictedStockpileDatas.TryGetValue(index, out var data))
                return data;

            return _stockpileDatas.GetRef(index);
        }

        public int FindFreeStockpileIndex()
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
            ref FStockpileData stockpile = ref _stockpileDatas.GetRef(stockpileIndex);
            int returnValue = stockpile.AddToStockpile(currencyType, value);

            if (returnValue > 0)
            {
                if (HasStateAuthority)
                {
                    pc.Currency.RPC_AddCurrency(currencyType, returnValue);
                }
            }
        }

        public void Predict_StockpileDropOff_Player(int stockpileIndex, ECurrencyType currencyType, int value)
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
                    Debug.Log("Use Predicted");
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

            // Optional: Debug log totals
            foreach (var kvp in _allCurrencies)
            {
                Debug.Log($"Currency: {kvp.Key}, Amount: {kvp.Value}");
            }
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
