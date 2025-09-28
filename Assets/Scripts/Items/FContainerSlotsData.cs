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
        private NetworkBool _isAssigned;

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

        public bool IsAssigned
        {
            get => _isAssigned;
            set => _isAssigned = value;
        }
    }
}
