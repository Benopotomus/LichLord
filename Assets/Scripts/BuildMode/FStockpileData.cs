using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;


namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct FStockpileData : INetworkStruct
    {
        [FieldOffset(0)] private ushort _wood;
        [FieldOffset(2)] private ushort _stone;
        [FieldOffset(4)] private ushort _iron;
        [FieldOffset(6)] private ushort _gold;
    }
}
