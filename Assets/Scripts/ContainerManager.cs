using Fusion;
using LichLord.Buildables;
using LichLord.World;
using UnityEngine;

namespace LichLord
{
    public class ContainerManager : ContextBehaviour
    {
        [Networked]
        [SerializeField]
        protected byte _freeStockpileIndex { get; set; }

        [Networked, Capacity(128)]
        protected virtual NetworkArray<FStockpileData> _stockPileDatas { get; }
        public int StockpileCount => _stockPileDatas.Length;

        public ref FStockpileData GetStockPile(int index)
        {
            return ref _stockPileDatas.GetRef(index);
        }

        public int AssignStockpileIndex()
        {
            int freeIndex = _freeStockpileIndex;
            _freeStockpileIndex++;
            return freeIndex;
        }

        public void LoadStockPileData(FStockpileSaveData stockpileSave)
        {
            ref FStockpileData stockpileData = ref _stockPileDatas.GetRef(stockpileSave.index);
            stockpileData = stockpileSave.ToNetworkStockpile();
            _stockPileDatas.Set(stockpileSave.index, stockpileData);
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
            ref FStockpileData stockpile = ref _stockPileDatas.GetRef(stockpileIndex);
            int returnValue = stockpile.AddToStockpile(currencyType, value);

            if (returnValue > 0)
            {
                if (HasStateAuthority)
                {
                    pc.Currency.RPC_AddCurrency(currencyType, returnValue);
                }
            }

        }

    }
}
