using Fusion;
using UnityEngine;

namespace LichLord
{
    public class PlayerCurrencyComponent : ContextBehaviour
    {
        // Fixed slot order for iteration/enumeration (if needed for UI/order).
        private static readonly ECurrencyType[] kSlotOrder =
        {
            ECurrencyType.Wood,
            ECurrencyType.Stone,
            ECurrencyType.IronOre,
            ECurrencyType.Gold,
            ECurrencyType.Souls,
            ECurrencyType.Deathcaps,
        };

        [SerializeField] private CurrencyDefinition _woodDefinition;
        [SerializeField] private CurrencyDefinition _stoneDefinition;
        [SerializeField] private CurrencyDefinition _ironDefinition;
        [SerializeField] private CurrencyDefinition _goldDefinition;
        [SerializeField] private CurrencyDefinition _soulsDefinition;
        [SerializeField] private CurrencyDefinition _deathcapsDefinition;

        [Networked, Capacity(16)]
        private NetworkDictionary<ECurrencyType, byte> _currencyAmounts => default;

        private const int CURRENCY_MAX = 250; // Fixed max for all currencies (can be per-type dict if needed later).

        public int CurrencyCount => kSlotOrder.Length; // Retained for compatibility; actual unique count is _currencyAmounts.Count.
        public int NonZeroCurrencyCount => GetNonZeroCount();

        public byte GetAmount(ECurrencyType type) => _currencyAmounts.TryGet(type, out byte amount) ? amount : (byte)0;

        public override void Spawned()
        {
            base.Spawned();

            // No fixed init needed; dict starts empty. Optionally pre-populate with 0s for known types.
            foreach (var type in kSlotOrder)
            {
                _currencyAmounts.Set(type, 0);
            }
        }

        public bool HasRoomForCurrency(ECurrencyType type, int amount)
        {
            if (type == ECurrencyType.None) return false;
            var current = GetAmount(type);
            return CURRENCY_MAX >= (current + amount);
        }

        public void GetCurrencyWithCount(ref ECurrencyType currencyType, ref int value)
        {
            foreach (var kvp in _currencyAmounts)
            {
                if (kvp.Value > 0)
                {
                    currencyType = kvp.Key;
                    value = kvp.Value;
                    return;
                }
            }
            currencyType = ECurrencyType.None;
            value = 0;
        }

        public void AddCurrency(ECurrencyType type, int amount)
        {
            if (type == ECurrencyType.None || amount <= 0) return;

            var current = GetAmount(type);
            var newAmount = (byte)Mathf.Min(current + amount, CURRENCY_MAX);
            _currencyAmounts.Set(type, newAmount);
        }

        public CurrencyDefinition GetCurrencyDefinition(ECurrencyType type)
        {
            return type switch
            {
                ECurrencyType.Wood => _woodDefinition,
                ECurrencyType.Stone => _stoneDefinition,
                ECurrencyType.IronOre => _ironDefinition,
                ECurrencyType.Gold => _goldDefinition,
                ECurrencyType.Souls => _soulsDefinition,
                ECurrencyType.Deathcaps => _deathcapsDefinition,
                _ => null
            };
        }

        public int GetCurrencyCount(ECurrencyType type)
        {
            return GetAmount(type);
        }

        public int GetCurrencyMax(ECurrencyType type)
        {
            return type != ECurrencyType.None ? CURRENCY_MAX : 0;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_AddCurrency(ECurrencyType currencyType, int value)
        {
            AddCurrency(currencyType, value);
        }

        // Legacy compatibility: Get by fixed index (maps to kSlotOrder).
        public FCurrencyStack GetStackAtIndex(int index)
        {
            if (index < 0 || index >= kSlotOrder.Length)
                return default;

            var type = kSlotOrder[index];
            var amount = GetAmount(type);
            return new FCurrencyStack { CurrencyType = type, Value = amount };
        }

        private int GetNonZeroCount()
        {
            int count = 0;
            foreach (var kvp in _currencyAmounts)
            {
                if (kvp.Value > 0) count++;
            }
            return count;
        }
    }
}