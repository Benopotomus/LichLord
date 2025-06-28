using UnityEngine;
using Fusion;
using System.Collections.Generic;
using LichLord.Props;

namespace LichLord.World
{
    [System.Serializable]
    public class Chunk
    {
        private ChunkManager _manager;

        private List<IChunkTrackable> _trackablesInChunk = new List<IChunkTrackable>();
        public List<IChunkTrackable> Trackables => _trackablesInChunk;

        private List<PropRuntimeState> _propStatesInChunk = new List<PropRuntimeState>(); // Store prop states
        public List<PropRuntimeState> PropStates => _propStatesInChunk;

        private Dictionary<int, PropRuntimeState> _deltaPropStates = new Dictionary<int, PropRuntimeState>();
        public Dictionary<int, PropRuntimeState> DeltaPropStates => _deltaPropStates;

        public FChunkPosition ChunkID { get; set; }
        public Bounds Bounds { get; set; }

        public ELoadState LoadState { get; set; }

        public Chunk(FChunkPosition chunkID, Vector2 worldOrigin, ChunkManager manager)
        {
            ChunkID = chunkID;
            _manager = manager;

            float chunkSize = WorldConstants.CHUNK_SIZE;

            // Calculate the chunk's world position, accounting for worldOrigin as the center
            Vector2 chunkCorner = new Vector2(
                worldOrigin.x + chunkID.X * chunkSize,
                worldOrigin.y + chunkID.Y * chunkSize
            );

            // Set Bounds center at the middle of the chunk
            Vector2 center = new Vector2(
                chunkCorner.x + chunkSize / 2,
                chunkCorner.y + chunkSize / 2
            );

            // Create Bounds with height 1000 (as in original) for 3D space
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

        public void AddOrUpdateDeltaState(PropRuntimeState propState)
        {
            _deltaPropStates[propState.guid] = propState;
            _manager.DeltaChunks.Add(this);
        }
    }
}