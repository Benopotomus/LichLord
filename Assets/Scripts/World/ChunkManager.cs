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
        private ChunkReplicator _replicatorPrefab;

        [SerializeField]
        private bool drawChunkBounds = true;

        private Chunk[,] _worldChunks = new Chunk[135, 135];
        public Chunk[,] WorldChunks => _worldChunks;

        public HashSet<Chunk> _deltaChunks = new HashSet<Chunk>();
        public HashSet<Chunk> DeltaChunks => _deltaChunks;

        private HashSet<Chunk> _replicatedChunks = new HashSet<Chunk>(); // Track replicated chunks
        public HashSet<Chunk> ReplicatedChunks => _replicatedChunks;

        private HashSet<Chunk> _loadedChunks = new HashSet<Chunk>(); // Track loaded chunks
        public HashSet<Chunk> LoadedChunks => _loadedChunks;

        private List<ChunkReplicator> _replicators = new List<ChunkReplicator>();

        private Stack<ChunkReplicator> _replicatorPool = new Stack<ChunkReplicator>();

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
                        X = (byte)x,
                        Y = (byte)y
                    };

                    Chunk chunk = new Chunk(chunkID, this);

                    _worldChunks[x, y] = chunk;

                    if (chunk.LoadState == ELoadState.None)
                    {
                        LoadChunkIntoMemory(chunk);
                    }
                }
            }
        }

        public void TryAddReplicatedChunks(List<Chunk> chunksToAdd)
        {
            if (chunksToAdd == null || chunksToAdd.Count == 0)
                return;

            foreach (var chunk in chunksToAdd)
            {
                if (chunk == null)
                    continue;

                chunk.IncrementReplicationRef();

                _replicatedChunks.Add(chunk);

                if (HasStateAuthority)
                {
                    var chunkId =  chunk.ChunkID;

                    foreach (var activeReplicator in _replicators)
                    {
                        if (activeReplicator.ChunkID.IsEqual(ref chunkId))
                            return;
                    }

                    ChunkReplicator replicator;

                    // Reuse from pool
                    if (_replicatorPool.Count > 0)
                    {
                        replicator = _replicatorPool.Pop();
                        replicator.transform.position = chunk.Bounds.center;
                        replicator.SetID(chunk.ChunkID);
                        replicator.gameObject.SetActive(true); // Enable reused replicator

                    }
                    else
                    {
                        replicator = Runner.Spawn(_replicatorPrefab,
                            chunk.Bounds.center,
                            Quaternion.identity,
                            inputAuthority: null,
                            onBeforeSpawned: (runner, spawnedObject) =>
                            {
                                var rep = spawnedObject.GetComponent<ChunkReplicator>();
                                rep.SetID(chunk.ChunkID);
                            });
                    }
                }
            }
        }

        public void TryRemoveReplicatedChunks(List<Chunk> chunksToRemove)
        {
            if (chunksToRemove == null || chunksToRemove.Count == 0)
                return;

            foreach (var chunk in chunksToRemove)
            {
                if (chunk == null)
                    continue;

                chunk.DecrementReplicationRef();

                if (chunk.ReplicationRefCount > 0)
                    continue;

                _replicatedChunks.Remove(chunk);

                if (HasStateAuthority)
                {
                    var replicator = chunk.Replicator;
                    chunk.Replicator = null;

                    _replicatorPool.Push(replicator);
                    replicator.gameObject.SetActive(false);
                }
            }
        }
  
        private void LoadChunkIntoMemory(Chunk chunkToLoad)
        {
            chunkToLoad.LoadState = ELoadState.Loaded;
            Context.PropManager.LoadPropsForChunk(chunkToLoad);
            _loadedChunks.Add(chunkToLoad);
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
                X = (byte)Mathf.FloorToInt((position.x) / WorldConstants.CHUNK_SIZE),
                Y = (byte)Mathf.FloorToInt((position.z) / WorldConstants.CHUNK_SIZE)
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

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (!Context.IsGameplayActive())
                return;

            int tick = Runner.Tick;

            foreach (Chunk chunk in _replicatedChunks)
            {
                chunk.OnFixedUpdateNetwork(tick);
            }
        }

        int _lastTick = -1;
        public override void Render()
        {
            if (_lastTick == Runner.Tick)
                return;

            _lastTick = Runner.Tick;

            if (!Context.IsGameplayActive())
                return;

            var pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            Debug.Log(Runner.GetAllBehaviours<ChunkReplicator>().Count);
            foreach (ChunkReplicator replicator in _replicators)
            {
                replicator.OnRender();
            }

            float renderDeltaTime = Runner.LocalAlpha;
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            // Update states from player's cached list
            foreach (Chunk chunk in pc.CachedChunks)
            {
                chunk.OnRender(hasAuthority, renderDeltaTime);
            }
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

        // Just on spawned callbacks so we have a list
        public void RegisterReplicator(ChunkReplicator replicator)
        {
            _replicators.Add(replicator);
        }
    }
}
