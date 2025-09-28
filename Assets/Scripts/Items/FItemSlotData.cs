namespace LichLord.Items
{
    using Fusion;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Size = 7)]
    public struct FItemSlotData : INetworkStruct
    {
        [FieldOffset(0)]
        public FItemData ItemData; // 6 bytes
        [FieldOffset(6)]
        public NetworkBool IsAssigned; // 1 byte



    }
}
