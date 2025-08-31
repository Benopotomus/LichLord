using Fusion;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct FCurrencyStack : INetworkStruct
    {
        [FieldOffset(0)] private ECurrencyType _currencyType;
        [FieldOffset(1)] private byte _value;

        public ECurrencyType CurrencyType
        {
            get => _currencyType;
            set => _currencyType = value;
        }

        public byte Value
        {
            get => _value;
            set => _value = value;
        }

        public bool IsEmpty() => _currencyType == ECurrencyType.None || _value == 0;
    }

    [StructLayout(LayoutKind.Explicit, Size = 9)]
    public struct FStockpileData : INetworkStruct
    {
        [FieldOffset(0)] private FCurrencyStack _pile0;
        [FieldOffset(2)] private FCurrencyStack _pile1;
        [FieldOffset(4)] private FCurrencyStack _pile2;
        [FieldOffset(6)] private FCurrencyStack _pile3;
        [FieldOffset(8)] private NetworkBool _isAssigned;

        /// <summary>
        /// Whether this stockpile is currently assigned
        /// </summary>
        public bool IsAssigned
        {
            get => _isAssigned;
            set => _isAssigned = value;
        }

        public void Assign() => _isAssigned = true;
        public void Unassign() => _isAssigned = false;

        public int AddToStockpile(ECurrencyType currencyType, int value)
        {
            const byte MAX_PILE_AMOUNT = 250;

            // Copy piles into an array
            FCurrencyStack[] piles = { _pile0, _pile1, _pile2, _pile3 };

            int remaining = value;

            // Pass 1: Fill existing piles of the same currency
            for (int i = 0; i < piles.Length && remaining > 0; i++)
            {
                if (piles[i].CurrencyType == currencyType)
                {
                    int space = MAX_PILE_AMOUNT - piles[i].Value;
                    if (space > 0)
                    {
                        int toAdd = Mathf.Min(space, remaining);
                        piles[i].Value = (byte)(piles[i].Value + toAdd);
                        remaining -= toAdd;
                    }
                }
            }

            // Pass 2: Fill empty piles
            for (int i = 0; i < piles.Length && remaining > 0; i++)
            {
                if (piles[i].CurrencyType == ECurrencyType.None) // Assuming None means empty
                {
                    int toAdd = Mathf.Min(MAX_PILE_AMOUNT, remaining);
                    piles[i].CurrencyType = currencyType;
                    piles[i].Value = (byte)toAdd;
                    remaining -= toAdd;
                }
            }

            // Write piles back to struct
            _pile0 = piles[0];
            _pile1 = piles[1];
            _pile2 = piles[2];
            _pile3 = piles[3];

            return remaining; // Leftover amount
        }

        public FCurrencyStack GetCurrencyStack(int index) 
        {
            switch (index)
            { 
                case 0: return _pile0;
                case 1: return _pile1;
                case 2: return _pile2;
                case 3: return _pile3;
            }

            return _pile0;   
        }

        public int GetCurrencyAmount(ECurrencyType currencyType)
        {
            int total = 0;
            for (int i = 0; i < 4; i++)
            {
                FCurrencyStack pile = GetCurrencyStack(i);
                if (pile.CurrencyType == currencyType)
                    total += pile.Value;
            }
            return total;
        }

        public bool IsEmpty()
        {
            for (int i = 0; i < 4; i++)
            {
                var stack = GetCurrencyStack(i);
                if(!stack.IsEmpty())
                    return false;
            }
            return true;
        }

        public void ClearStockpile()
        {
            _pile0 = new FCurrencyStack { CurrencyType = ECurrencyType.None, Value = 0 };
            _pile1 = new FCurrencyStack { CurrencyType = ECurrencyType.None, Value = 0 };
            _pile2 = new FCurrencyStack { CurrencyType = ECurrencyType.None, Value = 0 };
            _pile3 = new FCurrencyStack { CurrencyType = ECurrencyType.None, Value = 0 };
        }

        public void Copy(FStockpileData other)
        {
            _pile0 = other._pile0;
            _pile1 = other._pile1;
            _pile2 = other._pile2;
            _pile3 = other._pile3;
            _isAssigned = other._isAssigned;
        }

        public bool IsEqual(FStockpileData other)
        {
            return _pile0.CurrencyType == other._pile0.CurrencyType &&
                   _pile0.Value == other._pile0.Value &&

                   _pile1.CurrencyType == other._pile1.CurrencyType &&
                   _pile1.Value == other._pile1.Value &&

                   _pile2.CurrencyType == other._pile2.CurrencyType &&
                   _pile2.Value == other._pile2.Value &&

                   _pile3.CurrencyType == other._pile3.CurrencyType &&
                   _pile3.Value == other._pile3.Value &&

                   _isAssigned == other._isAssigned;
        }

        public bool CanFit(ECurrencyType currencyType, int value)
        {
            const byte MAX_PILE_AMOUNT = 250;

            // Copy piles into a local array
            FCurrencyStack[] piles = { _pile0, _pile1, _pile2, _pile3 };

            int remaining = value;

            // Pass 1: Fill existing stacks of the same currency
            for (int i = 0; i < piles.Length && remaining > 0; i++)
            {
                if (piles[i].CurrencyType == currencyType)
                {
                    int space = MAX_PILE_AMOUNT - piles[i].Value;
                    if (space > 0)
                    {
                        int toAdd = Mathf.Min(space, remaining);
                        remaining -= toAdd;
                    }
                }
            }

            // Pass 2: Fill empty piles
            for (int i = 0; i < piles.Length && remaining > 0; i++)
            {
                if (piles[i].CurrencyType == ECurrencyType.None)
                {
                    int toAdd = Mathf.Min(MAX_PILE_AMOUNT, remaining);
                    remaining -= toAdd;
                }
            }

            // If nothing left, it fits
            return remaining <= 0;
        }
    }
}
