// Assets/Scripts/LichLord/TrackableData.cs
using Unity.Mathematics;

namespace LichLord
{
    public struct TrackableData
    {
        public float3 Position;
        public int TrackableIndex;
        public int ChunkIndex;
        public int TeamID;
        public byte Flags;
        public short HarvestPoints;
    }
}