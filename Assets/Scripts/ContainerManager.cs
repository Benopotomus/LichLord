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
        [Networked]
        [SerializeField]
        protected byte _freeStockpileIndex { get; set; }

        [Networked, Capacity(128)]
        [OnChangedRender(nameof(OnRep_StockpileDatas))]
        protected virtual NetworkArray<FStockpileData> _stockpileDatas { get; }

        private void OnRep_StockpileDatas()
        {
            UpdateAllCurrencies();
        }

        public int StockpileCount => _stockpileDatas.Length;

        private Dictionary<ECurrencyType, int> _allCurrencies = new Dictionary<ECurrencyType, int>();
        public Dictionary<ECurrencyType, int> AllCurrencies => _allCurrencies;

        public override void Spawned()
        {
            base.Spawned();
            UpdateAllCurrencies();
        }

        public ref FStockpileData GetStockPile(int index)
        {
            return ref _stockpileDatas.GetRef(index);
        }

        public int AssignStockpileIndex()
        {
            int freeIndex = _freeStockpileIndex;
            _freeStockpileIndex++;
            return freeIndex;
        }

        public void LoadStockPileData(FStockpileSaveData stockpileSave)
        {
            ref FStockpileData stockpileData = ref _stockpileDatas.GetRef(stockpileSave.index);
            stockpileData = stockpileSave.ToNetworkStockpile();
            _stockpileDatas.Set(stockpileSave.index, stockpileData);
        }

        public void LoadStockPileBuildable(int stockpileIndex)
        {
            if (!HasStateAuthority)
                return;

            if (_freeStockpileIndex <= stockpileIndex)
            {
                _freeStockpileIndex = (byte)(stockpileIndex + 1);
            }
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

        public int GetTotalCurrency(ECurrencyType currencyType)
        {
            int total = 0;
            for (int i = 0; i < _stockpileDatas.Length; i++)
            {
                // Use a ref to avoid extra copies
                ref FStockpileData stockpile = ref _stockpileDatas.GetRef(i);

                if (!stockpile.IsEmpty()) // optional if you have empty/default slots
                {
                    total += stockpile.GetCurrencyAmount(currencyType);
                }
            }
            return total;
        }

        public Dictionary<ECurrencyType, int> GetAllCurrencies()
        {
            var totals = new Dictionary<ECurrencyType, int>();

            for (int i = 0; i < _stockpileDatas.Length; i++)
            {
                ref FStockpileData stockpile = ref _stockpileDatas.GetRef(i);

                if (!stockpile.IsEmpty())
                {
                    foreach (ECurrencyType type in System.Enum.GetValues(typeof(ECurrencyType)))
                    {
                        int current = stockpile.GetCurrencyAmount(type);
                        if (current > 0)
                        {
                            if (totals.ContainsKey(type))
                                totals[type] += current;
                            else
                                totals[type] = current;
                        }
                    }
                }
            }

            return totals;
        }

        public void UpdateAllCurrencies()
        {
            _allCurrencies = GetAllCurrencies();
            foreach (var kvp in _allCurrencies)
            {
                Debug.Log($"Currency: {kvp.Key}, Amount: {kvp.Value}");
            }
        }
    }
}
