using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LichLord.NonPlayerCharacters;

namespace LichLord
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SensingJobSystem : ContextBehaviour
    {
        private readonly List<NonPlayerCharacterBrainComponent> _brains = new();

        public void QueueSense(NonPlayerCharacterBrainComponent brain)
        {
            if (brain.NPC.CurrentChunk.IsValid)
                _brains.Add(brain);
        }

        private void LateUpdate()
        {
            if (_brains.Count == 0) return;

            // === 1. Gather data (main thread, fast) ===
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
                    SenseRadiusSqr = 500f * 500f,
                    ChunkDataStart = chunkOffsets.Length,
                    ChunkCount = 0
                };

                var nearby = Context.ChunkManager.GetNearbyChunks(center.Chunk.ChunkID, 1);
                int validChunks = 0;

                for (int c = 0; c < nearby.Count && c < 9; c++)
                {
                    var chunk = nearby[c];
                    var arr = chunk.NativeTrackables;
                    if (!arr.IsCreated) continue;

                    chunkOffsets.Add(allTrackables.Length);
                    for (int t = 0; t < arr.Length; t++)
                    {
                        var track = arr[t];
                        track.ChunkIndex = c;
                        allTrackables.Add(track);
                    }
                    validChunks++;
                }

                var input = inputs[i];
                input.ChunkCount = validChunks;
                inputs[i] = input;
            }

            chunkOffsets.Add(allTrackables.Length);

            // === 2. Schedule job (runs on worker threads) ===
            var job = new SensingJob
            {
                Inputs = inputs,
                AllTrackables = allTrackables.AsDeferredJobArray(),
                ChunkOffsets = chunkOffsets.AsDeferredJobArray(),
                Results = results
            };

            JobHandle handle = job.Schedule(_brains.Count, 32);

            // === 3. Copy brains BEFORE clearing ===
            var brainsCopy = _brains.ToArray();
            _brains.Clear();

            // === 4. Start coroutine to wait & apply (NO BLOCK) ===
            StartCoroutine(ApplyWhenDone(handle, results, allTrackables, brainsCopy));
        }

        private IEnumerator ApplyWhenDone(
            JobHandle handle,
            NativeArray<SenseResult> results,
            NativeList<TrackableData> trackables,
            NonPlayerCharacterBrainComponent[] brains)
        {
            // Wait until job finishes (does NOT block main thread)
            while (!handle.IsCompleted)
                yield return null;

            // Now safe: job is done → Complete is instant
            handle.Complete();

            // === 5. Apply results ===
            for (int i = 0; i < brains.Length; i++)
            {
                var brain = brains[i];
                if (brain == null) continue;

                var result = results[i];

                if (brain.NPC.CurrentChunk.IsValid)
                {
                    var nearby = Context.ChunkManager.GetNearbyChunks(brain.NPC.CurrentChunk.Chunk.ChunkID, 1);
                    brain.ApplySenseResult(result, trackables, nearby);
                }
            }

            // === 6. Cleanup ===
            results.Dispose();
            trackables.Dispose();
        }
    }
}