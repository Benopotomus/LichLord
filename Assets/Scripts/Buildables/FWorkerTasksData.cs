using Fusion;
using System.Runtime.InteropServices;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct FWorkerTasksData : INetworkStruct
    {
        [FieldOffset(1)]
        private byte _harvestTypes;

        public bool Wood { get { return IsBitSet(ref _harvestTypes, 1); } set { SetBit(ref _harvestTypes, 1, value); } }
        public bool Stone { get { return IsBitSet(ref _harvestTypes, 2); } set { SetBit(ref _harvestTypes, 2, value); } }
        public bool Deathcaps { get { return IsBitSet(ref _harvestTypes, 3); } set { SetBit(ref _harvestTypes, 3, value); } }
        public bool IronOre { get { return IsBitSet(ref _harvestTypes, 4); } set { SetBit(ref _harvestTypes, 4, value); } }
        public bool Bones { get { return IsBitSet(ref _harvestTypes, 5); } set { SetBit(ref _harvestTypes, 5, value); } }
        public bool Transport { get { return IsBitSet(ref _harvestTypes, 6); } set { SetBit(ref _harvestTypes, 6, value); } }

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
