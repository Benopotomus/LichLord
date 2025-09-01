using Fusion;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord
{

    [StructLayout(LayoutKind.Explicit, Size = 9)]
    public struct FDialogData : INetworkStruct
    {
        [FieldOffset(0)] public int DialogID;
        [FieldOffset(4)] private byte _state;

        public bool IsAssigned { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }
        public bool NPCActive { get { return IsBitSet(ref _state, 2); } set { SetBit(ref _state, 2, value); } }
        public bool IsOpen { get { return IsBitSet(ref _state, 3); } set { SetBit(ref _state, 3, value); } }
        public bool IsAnswered { get { return IsBitSet(ref _state, 4); } set { SetBit(ref _state, 4, value); } }

        public bool IsBitSet(ref byte flags, int bit)
        {
            return (flags & (1 << bit)) == (1 << bit);
        }

        public byte SetBit(ref byte flags, int bit, bool value)
        {
            if (value == true)
            {
                return flags |= (byte)(1 << bit);
            }
            else
            {
                return flags &= unchecked((byte)~(1 << bit));
            }
        }

        public byte SetBitNoRef(byte flags, int bit, bool value)
        {
            if (value == true)
            {
                return flags |= (byte)(1 << bit);
            }
            else
            {
                return flags &= unchecked((byte)~(1 << bit));

            }
        }
    }
}