using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord.Items
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FDroppedItemData : INetworkStruct
    {
        [FieldOffset(0)]
        public FItemData ItemData; // 4
        [FieldOffset(4)]
        public FWorldPosition WorldPosition; // 7
        
    }
}
