using Fusion;
using System.Runtime.InteropServices;

namespace LichLord.Buildables
{

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildFloorData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte FloorDefinitionID;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildWallData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte WallDefinitionID;
    }

}
