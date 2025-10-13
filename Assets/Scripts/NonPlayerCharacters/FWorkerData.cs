using Fusion;
using System.Runtime.InteropServices;

namespace LichLord.NonPlayerCharacters
{
    [StructLayout(LayoutKind.Explicit, Size = 5)]
    public struct FWorkerData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte StrongholdID;
        [FieldOffset(1)]
        private byte _state;
        [FieldOffset(2)]
        public FWorkerTasksData TasksData;
        [FieldOffset(3)]
        public ushort NPCIndex;

        public bool IsAssigned { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }
        public bool WorkerActive { get { return IsBitSet(ref _state, 2); } set { SetBit(ref _state, 2, value); } }

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

    public enum ECommandTask : byte
    { 
        None,
        Wood,
        Stone,
        IronOre,
        Deathcaps,
        Bones,
        Fiber,
        Transport,

    }
}
