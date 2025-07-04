using Fusion;
using System.Runtime.InteropServices;

namespace LichLord.Buildables
{

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildTileData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte FloorDefinitionID;

        [FieldOffset(1)]
        public byte DetailsDefinitionID;

        [FieldOffset(2)]
        public byte Orientation;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildWallData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte InteriorTileDefinitionID;
    }

}
