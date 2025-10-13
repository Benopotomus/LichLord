using Fusion;
using System.Runtime.InteropServices;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 1)]
    public struct FWorkerTasksData : INetworkStruct
    {
        [FieldOffset(0)]
        private byte _data;

        public bool Task1 { get { return IsBitSet(ref _data, 1); } set { SetBit(ref _data, 1, value); } }
        public bool Task2 { get { return IsBitSet(ref _data, 2); } set { SetBit(ref _data, 2, value); } }
        public bool Task3 { get { return IsBitSet(ref _data, 3); } set { SetBit(ref _data, 3, value); } }
        public bool Task4 { get { return IsBitSet(ref _data, 4); } set { SetBit(ref _data, 4, value); } }
        public bool Task5 { get { return IsBitSet(ref _data, 5); } set { SetBit(ref _data, 5, value); } }
        public bool Task6 { get { return IsBitSet(ref _data, 6); } set { SetBit(ref _data, 6, value); } }
        public bool Task7 { get { return IsBitSet(ref _data, 7); } set { SetBit(ref _data, 7, value); } }
        public bool Task8 { get { return IsBitSet(ref _data, 8); } set { SetBit(ref _data, 8, value); } }

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
