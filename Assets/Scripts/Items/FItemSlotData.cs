namespace LichLord.Items
{
    using Fusion;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Size = 5)]
    public struct FItemSlotData : INetworkStruct
    {
        [FieldOffset(0)]
        public FItemData ItemData; // 4 bytes
        [FieldOffset(4)]
        public NetworkBool IsAssigned; // 1 byte

        public bool IsEqual(FItemSlotData other)
        {
            if (!ItemData.IsEqual(in other.ItemData))
                return false;

            if (IsAssigned != other.IsAssigned)
                return false;

            return true;
        }

        public void Copy(FItemSlotData other)
        {
            ItemData.Copy(in other.ItemData);
            IsAssigned = other.IsAssigned;
        }

    }
}
