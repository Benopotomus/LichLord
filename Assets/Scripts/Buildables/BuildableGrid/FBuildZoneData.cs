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

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildInteriorData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte InteriorDefinitionID;

        [FieldOffset(1)]
        public byte GridX;

        [FieldOffset(2)]
        public byte GridY;

        [FieldOffset(3)]
        public byte GridZ;

        [FieldOffset(4)]
        public EWallOrientation Orientation;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildFeatureData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte FeatureDefinitionID;

        [FieldOffset(1)]
        public byte SubGridX;

        [FieldOffset(2)]
        public byte GridY;

        [FieldOffset(3)]
        public byte SubGridZ;

        [FieldOffset(4)]
        public EWallOrientation Orientation;

        [FieldOffset(5)]
        public int Data;
    }
}
