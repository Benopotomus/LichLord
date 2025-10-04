namespace LichLord.Items
{
    using Fusion;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Size = 5)]
    public struct FContainerSlotData : INetworkStruct
    {
        [FieldOffset(0)]
        private ushort _startIndex; // 2 bytes
        [FieldOffset(2)]
        private ushort _endIndex; // 2 bytes
        [FieldOffset(4)]
        private byte _state;

        // Constants for bit masks

        public int StartIndex
        {
            get => _startIndex;
            set => _startIndex = (ushort)value;
        }

        public int EndIndex
        {
            get => _endIndex;
            set => _endIndex = (ushort)value;
        }

        public bool IsAssigned { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }
        public bool IsStockpile { get { return IsBitSet(ref _state, 2); } set { SetBit(ref _state, 2, value); } }

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

        public bool IsEqual(FContainerSlotData other)
        {
            if (IsAssigned != other.IsAssigned)
                return false;

            if(IsStockpile != other.IsStockpile)
                return false;

            if (_startIndex != other._startIndex)
                return false;

            if (_endIndex != other._endIndex)
                return false;

            return true;
        }

        public void Copy(FContainerSlotData other)
        {
            _state = other._state;
            _startIndex = other._startIndex;
            _endIndex = other._endIndex;
        }
    }
}
