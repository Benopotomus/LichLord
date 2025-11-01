// Assets/Scripts/LichLord/SenseInput.cs
using Unity.Mathematics;

namespace LichLord
{
    public struct SenseInput
    {
        public float3 Position;
        public int TeamID;
        public int IsWorker;
        public int CarriedItemID;
        public float SenseRadiusSqr;
        public int ChunkDataStart;   // start index in flat AllTrackables
        public int ChunkCount;       // number of valid chunks (0–9)
    }
}