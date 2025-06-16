using UnityEngine;
using Fusion;
using System.Collections.Generic;
using LichLord.Props;

namespace LichLord.World
{
    [System.Serializable]
    public class Chunk
    {
        private List<IChunkTrackable> _trackablesInChunk = new List<IChunkTrackable>();
        public List<IChunkTrackable> Trackables => _trackablesInChunk;

        private List<PropRuntimeState> _propStatesInChunk = new List<PropRuntimeState>(); // Store prop states
        public List<PropRuntimeState> PropStates => _propStatesInChunk;

        public FChunkPosition ChunkID { get; set; }
        public Bounds Bounds { get; set; }

        public ELoadState LoadState { get; set; }

        public Chunk(FChunkPosition chunkID, Vector2 worldOrigin)
        {
            ChunkID = chunkID;

            float chunkSize = WorldConstants.CHUNK_SIZE;
            Vector2 center = new Vector2(
                worldOrigin.x + chunkID.X * chunkSize + chunkSize / 2,
                worldOrigin.y + chunkID.Y * chunkSize + chunkSize / 2
            );

            Bounds = new Bounds(new Vector3(center.x, 0, center.y), new Vector3(chunkSize, 1000, chunkSize));
        }

        public void AddObject(IChunkTrackable objId)
        {
            if (!_trackablesInChunk.Contains(objId))
                _trackablesInChunk.Add(objId);
        }

        public void RemoveObject(IChunkTrackable objId)
        {
            _trackablesInChunk.Remove(objId);
        }

        public void AddObject(PropRuntimeState propState)
        {
            if (!_propStatesInChunk.Contains(propState))
                _propStatesInChunk.Add(propState);
        }

        public void RemoveObject(PropRuntimeState propState)
        {
            _propStatesInChunk.Remove(propState);
        }
    }
}