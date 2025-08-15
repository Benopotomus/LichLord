using Fusion;
using System.Runtime.InteropServices;

namespace LichLord.Buildables
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FGridTransform : INetworkStruct
    {
        [FieldOffset(0)]
        int _gridGuid;
        [FieldOffset(4)]
        byte _x;
        [FieldOffset(5)]
        byte _y;
        [FieldOffset(6)]
        byte _z;
        [FieldOffset(7)]
        EFacingDirection _direction; //8
    }

    public enum EFacingDirection : byte
    { 
        North,
        South,
        East,
        West,
    }
}
