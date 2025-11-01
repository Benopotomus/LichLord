// Assets/Scripts/LichLord/SensingJob.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace LichLord
{
    [BurstCompile]
    public struct SensingJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<SenseInput> Inputs;
        [ReadOnly] public NativeArray<TrackableData> AllTrackables;
        [ReadOnly] public NativeArray<int> ChunkOffsets; // 9 per brain
        public NativeArray<SenseResult> Results;

        public void Execute(int i)
        {
            var input = Inputs[i];
            float3 pos = input.Position;

            float bestA = float.MaxValue, bestH = float.MaxValue, bestD = float.MaxValue;
            int idA = -1, idH = -1, idD = -1;

            int start = input.ChunkDataStart;
            int count = input.ChunkCount;

            for (int c = 0; c < count; c++)
            {
                int offset = ChunkOffsets[start + c];
                int nextOffset = (c + 1 < count) ? ChunkOffsets[start + c + 1] : AllTrackables.Length;

                for (int t = offset; t < nextOffset; t++)
                {
                    var track = AllTrackables[t];
                    float3 delta = track.Position - pos;
                    float d = math.lengthsq(delta);
                    if (d > input.SenseRadiusSqr) continue;

                    if ((track.Flags & 1) != 0 && track.TeamID != input.TeamID)
                        if (d < bestA) { bestA = d; idA = t; }

                    if (input.IsWorker == 1 && input.CarriedItemID == 0 && (track.Flags & 2) != 0 && track.HarvestPoints > 0)
                        if (d < bestH) { bestH = d; idH = t; }

                    if (input.IsWorker == 1 && input.CarriedItemID != 0 && (track.Flags & 4) != 0)
                        if (d < bestD) { bestD = d; idD = t; }
                }
            }

            Results[i] = new SenseResult
            {
                BrainIndex = i,
                AttackGlobalIndex = idA,
                HarvestGlobalIndex = idH,
                DepositGlobalIndex = idD,
                AttackDistSqr = bestA,
                HarvestDistSqr = bestH,
                DepositDistSqr = bestD
            };
        }
    }
}