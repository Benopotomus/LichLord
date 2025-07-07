using Fusion;
using System.Runtime.InteropServices;

namespace LichLord.Buildables
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildFloorData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte FloorDefinitionID;

        [FieldOffset(1)]
        public byte GridX;

        [FieldOffset(2)]
        public byte GridY;

        [FieldOffset(3)]
        public byte GridZ;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildWallData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte WallDefinitionID;

        [FieldOffset(1)]
        public byte GridX;

        [FieldOffset(2)]
        public byte GridY;

        [FieldOffset(3)]
        public byte GridZ;
    }

}
