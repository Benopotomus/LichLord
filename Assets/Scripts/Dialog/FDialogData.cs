using Fusion;
using System.Runtime.InteropServices;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 3)]
    public struct FDialogData : INetworkStruct
    {
        [FieldOffset(0)] public ushort DefinitionID;
        [FieldOffset(2)] private byte _state;

        public bool IsAssigned { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }

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