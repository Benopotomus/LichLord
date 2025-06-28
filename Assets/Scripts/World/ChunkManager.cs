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
        private bool drawChunkBounds = true; // Toggle for gizmo drawing

        private Chunk[,] _worldChunks = new Chunk[40, 40];
        public Chunk[,] WorldChunks => _worldChunks;

        public HashSet<Chunk> _deltaChunks = new HashSet<Chunk>();
        public HashSet<Chunk> DeltaChunks => _deltaChunks;

        private int _arrayOffsetX;  // Offset to handle negative X coordinates
        private int _arrayOffsetY;  // Offset to handle negative Y coordinates

        public void InitializeWorldChunks()
        {
            WorldSettings worldSettings = Context.WorldManager.WorldSettings;

            // Calculate chunk grid size based on world size and chunk size
            var chunkGridSize = new Vector2Int(
                Mathf.CeilToInt(worldSettings.WorldSize.x / WorldConstants.CHUNK_SIZE),
                Mathf.CeilToInt(worldSettings.WorldSize.y / WorldConstants.CHUNK_SIZE)
            );

            // Define min/max coordinates to handle negative sbyte values
            int minX = -chunkGridSize.x / 2; // Center the world around (0,0)
            int maxX = chunkGridSize.x / 2 - 1;
            int minY = -chunkGridSize.y / 2;
            int maxY = chunkGridSize.y / 2 - 1;

            // Set offsets to map negative coordinates to array indices
            _arrayOffsetX = -minX;
            _arrayOffsetY = -minY;

            // Initialize the chunk array
            _worldChunks = new Chunk[chunkGridSize.x, chunkGridSize.y];

            // Populate the chunk array and dictionaries
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    FChunkPosition chunkID = new FChunkPosition
                    {
                        X = (sbyte)x,
                        Y = (sbyte)y
                    };

                    // Map chunk coordinates to array indices
                    int arrayX = x + _arrayOffsetX;
                    int arrayY = y + _arrayOffsetY;

                    // Initialize chunk and store in array
                    _worldChunks[arrayX, arrayY] = new Chunk(chunkID, worldSettings.WorldOrigin, this);
                }
            }
        }

        public void LoadChunkFromSaves()
        {
            Dictionary<FChunkPosition, FChunkSaveData> loadedChunks =
                       Context.WorldSaveLoadManager.LoadedChunks;

            foreach(var chunkSaveData in loadedChunks.Values)
            {
                Chunk chunk = GetChunk(chunkSaveData.chunkCoord);
                foreach (var savedProp in chunkSaveData.props)
                {
                    // Copy the data
                    FPropData savedData = new FPropData();
                    savedData.StateData = savedProp.stateData;

                    // write out the props into the chunk itself
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
            if (!Runner.IsForward 
                || !Runner.IsFirstTick)
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
            //Get the local player's chunk and do a refresh if its closer
            if (PlayerCharacter.TryGetLocalPlayer(Runner, out PlayerCharacter character))
            {
                if (character.CurrentChunk == null)
                    return;

                FChunkPosition currentChunkId = character.CurrentChunk.ChunkID;
                FChunkPosition loadedChunkId = chunkToLoad.ChunkID;

                //Debug.Log("LoadChunk " + chunkToLoad.ChunkID);
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
            int arrayX = position.X + _arrayOffsetX;
            int arrayY = position.Y + _arrayOffsetY;

            // Check array bounds
            if (arrayX >= 0 && arrayX < _worldChunks.GetLength(0) &&
                arrayY >= 0 && arrayY < _worldChunks.GetLength(1))
            {
                return _worldChunks[arrayX, arrayY];
            }

            return null; // Return null if the chunk doesn't exist or is out of bounds
        }

        public Chunk GetChunkAtPosition(Vector3 position)
        {
            //
            return GetChunk(GetChunkID(position));
        }

        private FChunkPosition GetChunkID(Vector3 position)
        {
            WorldSettings worldSettings = Context.WorldManager.WorldSettings;

            return new FChunkPosition 
            {
                X = (sbyte)Mathf.FloorToInt((position.x - worldSettings.WorldOrigin.x) / WorldConstants.CHUNK_SIZE),
                Y = (sbyte)Mathf.FloorToInt((position.z - worldSettings.WorldOrigin.y) / WorldConstants.CHUNK_SIZE)
            };
        }

        public List<Chunk> GetNearbyChunks(FChunkPosition centerChunkID, int radius = 1)
        {
            List<Chunk> nearbyChunks = new List<Chunk>(capacity: (2 * radius + 1) * (2 * radius + 1));

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int arrayX = centerChunkID.X + dx + _arrayOffsetX;
                    int arrayY = centerChunkID.Y + dy + _arrayOffsetY;

                    // Clamp to array bounds
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
                if(chunk == null) 
                    continue;

                if (chunk.LoadState == ELoadState.Loaded)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.red;

                Bounds bounds = chunk.Bounds;
                // Draw wireframe cube for chunk bounds (XZ plane, height ignored for visualization)
                Gizmos.DrawWireCube(
                    new Vector3(bounds.center.x, 0, bounds.center.z),
                    new Vector3(bounds.size.x - 0.1f, 0.1f, bounds.size.z - 0.1f)
                );
            }
        }
    }
}