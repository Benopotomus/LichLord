using Fusion;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.Playables;

namespace LichLord.World
{
    public class ChunkManager : ContextBehaviour
    {
        [SerializeField]
        private WorldSettings _worldSettings;

        [SerializeField]
        private bool drawChunkBounds = true; // Toggle for gizmo drawing

        [Networked, Capacity(WorldConstants.CHUNK_COUNT_MAX)]
        public NetworkDictionary<FChunkPosition, ELoadState> _networkChunks { get; }
        private Dictionary<FChunkPosition, Chunk> _localChunks = new Dictionary<FChunkPosition, Chunk>();

        public override void Spawned()
        {
            base.Spawned();
            InitializeWorldChunks();
        }

        private void InitializeWorldChunks()
        {
            var chunkGridSize = new Vector2Int(
                Mathf.CeilToInt(_worldSettings.WorldSize.x / WorldConstants.CHUNK_SIZE),
                Mathf.CeilToInt(_worldSettings.WorldSize.y / WorldConstants.CHUNK_SIZE)
                );

            for (int x = 0; x < chunkGridSize.x; x++)
            {
                for (int y = 0; y < chunkGridSize.y; y++)
                {
                    FChunkPosition chunkID = new FChunkPosition 
                    { 
                        X = (sbyte)x, 
                        Y = (sbyte)y
                    };

                    _localChunks[chunkID] = new Chunk(chunkID, _worldSettings.WorldOrigin);

                    if (!_networkChunks.ContainsKey(chunkID))
                    {
                        _networkChunks.Set(chunkID, ELoadState.None);
                    }
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            var activePlayers = Context.NetworkGame.ActivePlayers;
            var chunksToMark = new HashSet<FChunkPosition>();

            // Collect all nearby chunk IDs to mark as Loaded
            for (int i = 0; i < activePlayers.Count; i++)
            {
                var player = activePlayers[i];
                if (player.CurrentChunk == null)
                    continue;

                var centerID = player.CurrentChunk.ChunkID;
                var nearby = GetNearbyChunks(centerID, radius: 3);

                for (int j = 0; j < nearby.Count; j++)
                {
                    chunksToMark.Add(nearby[j].ChunkID);
                }
            }

            // Set them in the network dictionary
            foreach (var chunkID in chunksToMark)
            {
                _networkChunks.Set(chunkID, ELoadState.Loaded);
            }
        }

        public override void Render()
        {
            base.Render();

            foreach (var networkChunk in _networkChunks)
            {
                // Get the local chunk for that value
                if (_localChunks.TryGetValue(networkChunk.Key, out Chunk localChunk))
                {
                    if (localChunk.LoadState == ELoadState.None &&
                        networkChunk.Value == ELoadState.Loaded)
                    {
                        LoadChunkIntoMemory(localChunk);
                    }
                }
            }
        }

        private void LoadChunkIntoMemory(Chunk chunkToLoad)
        {
            if (chunkToLoad.LoadState != ELoadState.None)
            {
                Debug.Log("Trying to load chunk but its not ready");
                return;
            }

            //Get the local player's chunk and do a refresh if its closer
            if (PlayerCharacter.TryGetLocalPlayer(Runner, out PlayerCharacter character))
            {
                if (character.CurrentChunk == null)
                    return;

                FChunkPosition currentChunkId = character.CurrentChunk.ChunkID;
                FChunkPosition loadedChunkId = chunkToLoad.ChunkID;

                //Debug.Log("LoadChunk " + chunkToLoad.ChunkID);
                chunkToLoad.LoadState = ELoadState.Loaded;
                Context.PropManager.LoadPropsForChunk(chunkToLoad.ChunkID);

                if (Math.Abs(currentChunkId.X - loadedChunkId.X) <= 2 &&
                    Math.Abs(currentChunkId.Y - loadedChunkId.Y) <= 2)
                {
                    character.UpdateVisibilePropStates();
                }
            }
        }

        public Chunk GetChunkAtPosition(Vector3 position)
        {
            if (_localChunks.TryGetValue(GetChunkID(position), out Chunk chunk))
                return chunk;

            return null;
        }

        private FChunkPosition GetChunkID(Vector3 position)
        {
            return new FChunkPosition 
            {
                X = (sbyte)Mathf.FloorToInt((position.x - _worldSettings.WorldOrigin.x) / WorldConstants.CHUNK_SIZE),
                Y = (sbyte)Mathf.FloorToInt((position.z - _worldSettings.WorldOrigin.y) / WorldConstants.CHUNK_SIZE)
            };
        }

        public List<Chunk> GetNearbyChunks(FChunkPosition centerChunkID, int radius = 1)
        {
            List<Chunk> nearbyChunks = new List<Chunk>();

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    FChunkPosition checkID = new FChunkPosition
                    {
                        X = (sbyte)(centerChunkID.X + dx),
                        Y = (sbyte)(centerChunkID.Y + dy)
                    };

                    if (_localChunks.TryGetValue(checkID, out Chunk chunk))
                    {
                        nearbyChunks.Add(chunk);
                    }
                }
            }

            return nearbyChunks;
        }

        void OnDrawGizmos()
        {
            if (!drawChunkBounds) return;

            foreach (var pair in _localChunks)
            {
                if (pair.Value.LoadState == ELoadState.Loaded)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.red;

                Bounds bounds = pair.Value.Bounds;
                // Draw wireframe cube for chunk bounds (XZ plane, height ignored for visualization)
                Gizmos.DrawWireCube(
                    new Vector3(bounds.center.x, 0, bounds.center.z),
                    new Vector3(bounds.size.x - 0.1f, 0.1f, bounds.size.z - 0.1f)
                );
            }
        }
    }
}