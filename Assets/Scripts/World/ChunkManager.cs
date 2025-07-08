using Fusion;
using System.Collections.Generic;
using UnityEngine;
using System;
using LichLord.Props;

namespace LichLord.World
{
    public class ChunkManager : ContextBehaviour
    {
        [SerializeField]
        private bool drawChunkBounds = true;

        private Chunk[,] _worldChunks = new Chunk[40, 40];
        public Chunk[,] WorldChunks => _worldChunks;

        public HashSet<Chunk> _deltaChunks = new HashSet<Chunk>();
        public HashSet<Chunk> DeltaChunks => _deltaChunks;

        public void InitializeWorldChunks()
        {
            WorldSettings worldSettings = Context.WorldManager.WorldSettings;

            var chunkGridSize = new Vector2Int(
                Mathf.CeilToInt(worldSettings.WorldSize.x / WorldConstants.CHUNK_SIZE),
                Mathf.CeilToInt(worldSettings.WorldSize.y / WorldConstants.CHUNK_SIZE)
            );

            _worldChunks = new Chunk[chunkGridSize.x, chunkGridSize.y];

            for (int x = 0; x < chunkGridSize.x; x++)
            {
                for (int y = 0; y < chunkGridSize.y; y++)
                {
                    FChunkPosition chunkID = new FChunkPosition
                    {
                        X = (sbyte)x,
                        Y = (sbyte)y
                    };

                    _worldChunks[x, y] = new Chunk(chunkID, this);
                }
            }
        }

        public void LoadChunksFromSaves()
        {
            Dictionary<FChunkPosition, FChunkSaveData> loadedChunks = Context.WorldSaveLoadManager.LoadedChunks;

            foreach (var chunkSaveData in loadedChunks.Values)
            {
                Chunk chunk = GetChunk(chunkSaveData.chunkCoord);
                foreach (var savedProp in chunkSaveData.props)
                {
                    FPropData savedData = new FPropData { StateData = savedProp.stateData };

                    PropRuntimeState propRuntimeState = new PropRuntimeState(
                        savedProp.guid,
                        savedProp.position,
                        savedProp.rotation,
                        savedProp.definitionId,
                        savedData
                    );

                    Context.PropManager.ReplicateAuthorityData(propRuntimeState);
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Runner.IsForward || !Runner.IsFirstTick)
                return;

            base.FixedUpdateNetwork();
        }

        public override void Render()
        {
            base.Render();

            if (!PlayerCharacter.TryGetLocalPlayer(Runner, out PlayerCharacter character))
                return;

            foreach (Chunk chunk in character.CachedChunks)
            {
                if (chunk.LoadState != ELoadState.None)
                    continue;

                LoadChunkIntoMemory(chunk);
            }
        }

        private void LoadChunkIntoMemory(Chunk chunkToLoad)
        {
            if (PlayerCharacter.TryGetLocalPlayer(Runner, out PlayerCharacter character))
            {
                if (character.CurrentChunk == null)
                    return;

                FChunkPosition currentChunkId = character.CurrentChunk.ChunkID;
                FChunkPosition loadedChunkId = chunkToLoad.ChunkID;

                chunkToLoad.LoadState = ELoadState.Loaded;
                Context.PropManager.LoadPropsForChunk(chunkToLoad);

                if (Math.Abs(currentChunkId.X - loadedChunkId.X) <= 2 &&
                    Math.Abs(currentChunkId.Y - loadedChunkId.Y) <= 2)
                {
                    character.UpdateVisibilePropStates();
                }
            }
        }

        public Chunk GetChunk(FChunkPosition position)
        {
            int arrayX = position.X;
            int arrayY = position.Y;

            if (arrayX >= 0 && arrayX < _worldChunks.GetLength(0) &&
                arrayY >= 0 && arrayY < _worldChunks.GetLength(1))
            {
                return _worldChunks[arrayX, arrayY];
            }

            return null;
        }

        public Chunk GetChunkAtPosition(Vector3 position)
        {
            return GetChunk(GetChunkID(position));
        }

        private FChunkPosition GetChunkID(Vector3 position)
        {
            WorldSettings worldSettings = Context.WorldManager.WorldSettings;

            return new FChunkPosition
            {
                X = (sbyte)Mathf.FloorToInt((position.x) / WorldConstants.CHUNK_SIZE),
                Y = (sbyte)Mathf.FloorToInt((position.z) / WorldConstants.CHUNK_SIZE)
            };
        }

        public List<Chunk> GetNearbyChunks(FChunkPosition centerChunkID, int radius = 1)
        {
            List<Chunk> nearbyChunks = new List<Chunk>((2 * radius + 1) * (2 * radius + 1));

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int arrayX = centerChunkID.X + dx;
                    int arrayY = centerChunkID.Y + dy;

                    if (arrayX >= 0 && arrayX < _worldChunks.GetLength(0) &&
                        arrayY >= 0 && arrayY < _worldChunks.GetLength(1))
                    {
                        Chunk chunk = _worldChunks[arrayX, arrayY];
                        if (chunk != null)
                        {
                            nearbyChunks.Add(chunk);
                        }
                    }
                }
            }

            return nearbyChunks;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
        }

        void OnDrawGizmos()
        {
            if (!drawChunkBounds) return;

            foreach (var chunk in _worldChunks)
            {
                if (chunk == null) continue;

                Gizmos.color = chunk.LoadState == ELoadState.Loaded ? Color.green : Color.red;

                Bounds bounds = chunk.Bounds;
                Gizmos.DrawWireCube(
                    new Vector3(bounds.center.x, 0, bounds.center.z),
                    new Vector3(bounds.size.x - 0.1f, 0.1f, bounds.size.z - 0.1f)
                );
            }
        }
    }
}
