using UnityEngine;
using Fusion;
using System.Collections.Generic;
using LichLord.Props;

namespace LichLord.World
{
    [System.Serializable]
    public class Chunk
    {
        public List<GameObject> ObjectsInChunk = new List<GameObject>();
        public List<PropRuntimeState> PropStates = new List<PropRuntimeState>(); // Store prop states

        public Vector2Int ChunkID { get; set; }
        public Bounds Bounds { get; set; }

        public ELoadState LoadState { get; set; }

        public Chunk(Vector2Int chunkID, Vector2 worldOrigin)
        {
            ChunkID = chunkID;

            float chunkSize = WorldConstants.CHUNK_SIZE;
            Vector2 center = new Vector2(
                worldOrigin.x + chunkID.x * chunkSize + chunkSize / 2,
                worldOrigin.y + chunkID.y * chunkSize + chunkSize / 2
            );

            Bounds = new Bounds(new Vector3(center.x, 0, center.y), new Vector3(chunkSize, 1000, chunkSize));
        }

        public void AddObject(GameObject objId)
        {
            if (!ObjectsInChunk.Contains(objId))
                ObjectsInChunk.Add(objId);
        }

        public void RemoveObject(GameObject objId)
        {
            ObjectsInChunk.Remove(objId);
        }

        public void AddObject(PropRuntimeState propState)
        {
            if (!PropStates.Contains(propState))
                PropStates.Add(propState);
        }

        public void RemoveObject(PropRuntimeState propState)
        {
            PropStates.Remove(propState);
        }
    }
}