// Assets/Scripts/LichLord/SensingJobSystem.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using LichLord.World;
using LichLord.NonPlayerCharacters;

namespace LichLord
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SensingJobSystem : ContextBehaviour
    {
        private readonly List<NonPlayerCharacterBrainComponent> _brains = new();

        public void QueueSense(NonPlayerCharacterBrainComponent brain)
        {
            if (brain?.NPC?.CurrentChunk != null)
                _brains.Add(brain);
        }

        private void Update()
        {
            if (_brains.Count == 0) return;

            var inputs = new NativeArray<SenseInput>(_brains.Count, Allocator.TempJob);
            var allTrackables = new NativeList<TrackableData>(Allocator.TempJob);
            var chunkOffsets = new NativeList<int>(Allocator.TempJob);
            var results = new NativeArray<SenseResult>(_brains.Count, Allocator.TempJob);

            for (int i = 0; i < _brains.Count; i++)
            {
                var brain = _brains[i];
                var npc = brain.NPC;
                var center = npc.CurrentChunk;

                inputs[i] = new SenseInput
                {
                    Position = npc.CachedTransform.position,
                    TeamID = (int)npc.TeamID,
                    IsWorker = npc.RuntimeState.IsWorker() ? 1 : 0,
                    CarriedItemID = npc.CarriedItem.CarriedItem.Data,
                    SenseRadiusSqr = 50f * 50f,
                    BrainIndex = i,
                    ChunkDataStart = chunkOffsets.Length,  // start of this brain's chunk offsets
                    ChunkCount = 0
                };

                var nearby = Context.ChunkManager.GetNearbyChunks(center.ChunkID, 1);
                int validChunks = 0;

                for (int c = 0; c < 9 && c < nearby.Count; c++)
                {
                    var chunk = nearby[c];
                    var arr = chunk.GetNativeTrackables();
                    if (!arr.IsCreated) continue;

                    // Record start of this chunk
                    chunkOffsets.Add(allTrackables.Length);

                    // Copy trackables with ChunkIndex
                    for (int t = 0; t < arr.Length; t++)
                    {
                        var track = arr[t];
                        track.ChunkIndex = c;  // SET CHUNK INDEX
                        allTrackables.Add(track);
                    }

                    validChunks++;
                }

                // Update input with valid chunk count
                var input = inputs[i];
                input.ChunkCount = validChunks;
                inputs[i] = input;
            }

            // Final offset
            chunkOffsets.Add(allTrackables.Length);

            var job = new SensingJob
            {
                Inputs = inputs,
                AllTrackables = allTrackables.AsArray(),
                ChunkOffsets = chunkOffsets.AsArray(),
                Results = results
            };

            JobHandle handle = job.Schedule(_brains.Count, 32);
            handle.Complete();

            // Apply results
            for (int i = 0; i < _brains.Count; i++)
            {
                var result = results[i];
                var brain = _brains[i];
                var nearby = Context.ChunkManager.GetNearbyChunks(brain.NPC.CurrentChunk.ChunkID, 1);

                brain.ApplySenseResult(result, allTrackables, nearby);
            }

            // Cleanup
            inputs.Dispose();
            allTrackables.Dispose();
            chunkOffsets.Dispose();
            results.Dispose();
            _brains.Clear();
        }
    }
}