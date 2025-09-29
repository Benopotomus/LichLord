using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.World
{
    public class ChunkManager : ContextBehaviour 
    {

        [SerializeField] private ChunkReplicator_256 _replicator256Prefab;
        [SerializeField] private ChunkReplicator_512 _replicator512Prefab;


        private readonly Stack<ChunkReplicator> _pool256 = new();
        private readonly Stack<ChunkReplicator> _pool512 = new();

        [SerializeField]
        private bool drawChunkBounds = true;

        private Chunk[,] _worldChunks = new Chunk[WorldConstants.WORLD_CHUNK_LENGTH, WorldConstants.WORLD_CHUNK_LENGTH];
        public Chunk[,] WorldChunks => _worldChunks;

        public HashSet<Chunk> _deltaChunks = new HashSet<Chunk>();
        public HashSet<Chunk> DeltaChunks => _deltaChunks;

        private HashSet<Chunk> _replicatedChunks = new HashSet<Chunk>(); // Track replicated chunks
        public HashSet<Chunk> ReplicatedChunks => _replicatedChunks;

        private HashSet<Chunk> _loadedChunks = new HashSet<Chunk>(); // Track loaded chunks
        public HashSet<Chunk> LoadedChunks => _loadedChunks;

        private HashSet<ChunkReplicator> _replicators = new HashSet<ChunkReplicator>();

        public bool ChunksReady { get; private set; }
        public Action onChunksReady;

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

            ChunksReady = true;
            onChunksReady?.Invoke();
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

                if (HasStateAuthority)
                {
                    if (_replicatedChunks.Contains(chunk))
                        continue;

                    var size = GetOptimalReplicatorSizeForChunk(chunk);
                    ChunkReplicator replicator = TryGetOrSpawnReplicator(chunk, size);
                }

                _replicatedChunks.Add(chunk);
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

                chunk.DespawnProps();
                _replicatedChunks.Remove(chunk);

                ChunkReplicator oldReplicator = chunk.Replicator;


                if (HasStateAuthority)
                {
                    if (oldReplicator == null)
                    {
                        //Debug.Log("No replicator");
                        continue;
                    }
                    
                    switch (oldReplicator)
                    {
                        case ChunkReplicator_256 r256: _pool256.Push(r256); break;
                        case ChunkReplicator_512 r512: _pool512.Push(r512); break;
                    }

                    if (oldReplicator.gameObject != null)
                    {
                        oldReplicator.gameObject.SetActive(false);
                    }
                    else
                    {
                        Debug.LogWarning($"[ChunkManager] Replicator {oldReplicator} had null gameObject. Possibly despawned?");
                    }
                }
            }
        }
  
        private void LoadChunkIntoMemory(Chunk chunkToLoad)
        {
            chunkToLoad.LoadState = ELoadState.Loaded;
            Context.PropManager.LoadPropsForChunk(chunkToLoad);
            Context.InvasionManager.LoadInvasionSpawnPointsForChunk(chunkToLoad);
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

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            var pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            foreach (ChunkReplicator replicator in _replicators)
            {
                replicator.OnRender();
            }

            float renderDeltaTime = Time.deltaTime;
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            // Update states from player's cached list
            foreach (Chunk chunk in _replicatedChunks)
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

        private EReplicatorSize GetOptimalReplicatorSizeForChunk(Chunk chunk)
        {
            int propCount = chunk.PropStates.Count; // or use whatever metric you track
            if (propCount == 0)
                return EReplicatorSize.S0;
            if (propCount <= 256)
                return EReplicatorSize.S256;
            else
                return EReplicatorSize.S512;
        }

        private ChunkReplicator TryGetOrSpawnReplicator(Chunk chunk, EReplicatorSize size)
        {
            if(size == EReplicatorSize.S0)
                return null;

            Stack<ChunkReplicator> pool = size switch
            {
                EReplicatorSize.S256 => _pool256,
                EReplicatorSize.S512 => _pool512,
                _ => throw new System.ArgumentOutOfRangeException()
            };

            ChunkReplicator replicator;
            if (pool.Count > 0)
            {
                replicator = pool.Pop();
                replicator.transform.position = chunk.Bounds.center;
                replicator.SetID(chunk.ChunkID);
                replicator.gameObject.SetActive(true);
                return replicator;
            }

            // Spawn the correct type via Fusion
            replicator = size switch
            {

                EReplicatorSize.S256 => Runner.Spawn(_replicator256Prefab, chunk.Bounds.center, Quaternion.identity, null,
                onBeforeSpawned: (runner, obj) =>
                {
                    var r = obj.GetComponent<ChunkReplicator_256>();
                    r.SetID(chunk.ChunkID);
                }),
                EReplicatorSize.S512 => Runner.Spawn(_replicator512Prefab, chunk.Bounds.center, Quaternion.identity, null,
                onBeforeSpawned: (runner, obj) =>
                {
                    var r = obj.GetComponent<ChunkReplicator_512>();
                    r.SetID(chunk.ChunkID);
                }),
                _ => throw new System.Exception("Unhandled replicator size")
            };

            return replicator;
        }
        public enum EReplicatorSize
        {
            S0 = 0,
            S256 = 256,
            S512 = 512,
        }
    }
}
