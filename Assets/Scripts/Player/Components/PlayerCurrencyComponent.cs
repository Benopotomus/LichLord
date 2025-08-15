using Fusion;
using UnityEngine;

namespace LichLord
{
    public class PlayerCurrencyComponent : ContextBehaviour
    {
        // Fixed slot order (indexes 0..4)
        private static readonly ECurrencyType[] kSlotOrder =
        {
            ECurrencyType.Wood,
            ECurrencyType.Stone,
            ECurrencyType.Iron,
            ECurrencyType.Gold,   // index 3
            ECurrencyType.Souls,  // index 4
        };

        [SerializeField] private CurrencyDefinition _woodDefinition;
        [SerializeField] private CurrencyDefinition _stoneDefinition;
        [SerializeField] private CurrencyDefinition _ironDefinition;
        [SerializeField] private CurrencyDefinition _goldDefinition;
        [SerializeField] private CurrencyDefinition _soulsDefinition;

        [Networked, Capacity(5)]
        private NetworkArray<FCurrencyStack> _currencies => default;

        private int[] _currencyMax = { 100, 100, 100, 100, 100 };

        public int CurrencyCount => _currencies.Length;
        public FCurrencyStack GetStackAtIndex(int index) => _currencies[index];

        public override void Spawned()
        {
            base.Spawned();

            // Initialize each slot with its fixed currency type
            for (int i = 0; i < kSlotOrder.Length; i++)
            {
                _currencies.Set(i, new FCurrencyStack { CurrencyType = kSlotOrder[i], Value = 0 });
            }
        }

        public bool HasRoomForCurrency(ECurrencyType type, int amount)
        {
            int idx = TypeToIndex(type);
            if (idx < 0) return false;
            return _currencyMax[idx] >= (_currencies[idx].Value + amount);
        }

        public void GetCurrencyWithCount(ref ECurrencyType currencyType, ref int value)
        {
            for (int i = 0; i < _currencies.Length; i++)
            {
                var stack = _currencies[i];
                if (stack.Value > 0)
                {
                    currencyType = stack.CurrencyType;
                    value = stack.Value;
                    return;
                }
            }
            currencyType = ECurrencyType.None;
            value = 0;
        }

        public void AddCurrency(ECurrencyType type, int amount)
        {
            int idx = TypeToIndex(type);
            if (idx < 0) return;

            var stack = _currencies[idx];
            stack.Value = (byte)Mathf.Min(stack.Value + amount, _currencyMax[idx]);
            _currencies.Set(idx, stack);
        }

        public CurrencyDefinition GetCurrencyDefinition(ECurrencyType type)
        {
            return type switch
            {
                ECurrencyType.Wood => _woodDefinition,
                ECurrencyType.Stone => _stoneDefinition,
                ECurrencyType.Iron => _ironDefinition,
                ECurrencyType.Gold => _goldDefinition,
                ECurrencyType.Souls => _soulsDefinition,
                _ => null
            };
        }

        public int GetCurrencyCount(ECurrencyType type)
        {
            int idx = TypeToIndex(type);
            return idx >= 0 ? _currencies[idx].Value : 0;
        }

        public int GetCurrencyMax(ECurrencyType type)
        {
            int idx = TypeToIndex(type);
            return idx >= 0 ? _currencyMax[idx] : 0;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_AddCurrency(ECurrencyType currencyType, int value)
        {
            AddCurrency(currencyType, value);
        }

        private static int TypeToIndex(ECurrencyType type)
        {
            if (type == ECurrencyType.None) return -1;
            for (int i = 0; i < kSlotOrder.Length; i++)
                if (kSlotOrder[i] == type) return i;
            return -1;
        }
    }
}
