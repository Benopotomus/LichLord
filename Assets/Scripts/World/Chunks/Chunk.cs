// Assets/Scripts/LichLord/World/Chunk.cs
using UnityEngine;
using System.Collections.Generic;
using LichLord.Props;
using Unity.Collections;
using LichLord.NonPlayerCharacters;
using LichLord.Buildables;

namespace LichLord.World
{
    public partial class Chunk
    {
        private ChunkManager _manager;
        private SceneContext _context;
        private ChunkReplicator _replicator;
        public ChunkReplicator Replicator => _replicator;
        private bool _hasReplicator;

        private List<IChunkTrackable> _trackablesInChunk = new List<IChunkTrackable>();
        public List<IChunkTrackable> Trackables => _trackablesInChunk;

        private List<NonPlayerCharacter> _npcsInChunk = new List<NonPlayerCharacter>();
        public List<NonPlayerCharacter> NPCsInChunk => _npcsInChunk;

        private List<InvasionSpawnPoint> _invasionSpawnPoints = new List<InvasionSpawnPoint>();
        public List<InvasionSpawnPoint> InvasionSpawnPoints => _invasionSpawnPoints;

        private PropRuntimeState[] _propRuntimeStates = new PropRuntimeState[0];
        public PropRuntimeState[] PropStates => _propRuntimeStates;

        private Dictionary<int, PropRuntimeState> _deltaPropStates = new Dictionary<int, PropRuntimeState>();
        public Dictionary<int, PropRuntimeState> DeltaPropStates => _deltaPropStates;

        public FChunkPosition ChunkID { get; set; }
        public Bounds Bounds { get; set; }

        public ELoadState LoadState { get; set; }

        public FPropLoadState[] PropLoadStates = new FPropLoadState[0];

        public int ReplicationRefCount { get; private set; } = 0;

        [SerializeField] private Dictionary<int, PropRuntimeState> _localRuntimePropStates = new Dictionary<int, PropRuntimeState>();
        [SerializeField] private Dictionary<int, PropRuntimeState> _predictedStates = new Dictionary<int, PropRuntimeState>();

        private Dictionary<int, PropRuntimeState> _loadedPropStates = new Dictionary<int, PropRuntimeState>();
        public Dictionary<int, PropRuntimeState> LoadedPropStates => _loadedPropStates;

        // -----------------------------------------------------------------
        // NATIVE TRACKABLES — PERSISTENT, IN-PLACE UPDATE
        // -----------------------------------------------------------------
        private NativeArray<TrackableData> _nativeTrackables;
        private int _lastValidCount = 0;
        private bool _trackablesDirty = true;

        public NativeArray<TrackableData> NativeTrackables => _nativeTrackables;

        public void MarkTrackablesDirty() => _trackablesDirty = true;

        private int _rebuildFrameCounter = 0;
        private const int REBUILD_EVERY_FRAMES = 2;

        public void RebuildNativeTrackables()
        {
            int validCount = 0;
            for (int i = 0; i < _trackablesInChunk.Count; i++)
                if (_trackablesInChunk[i] != null)
                    validCount++;

            // Resize only if count changed
            if (!_nativeTrackables.IsCreated || validCount != _lastValidCount)
            {
                if (_nativeTrackables.IsCreated)
                    _nativeTrackables.Dispose();

                _nativeTrackables = new NativeArray<TrackableData>(validCount, Allocator.Persistent);
                _lastValidCount = validCount;
            }

            // In-place update
            int writeIndex = 0;
            for (int i = 0; i < _trackablesInChunk.Count; i++)
            {
                var t = _trackablesInChunk[i];
                if (t == null) continue;

                _nativeTrackables[writeIndex++] = new TrackableData
                {
                    Position = t.Position,
                    TrackableIndex = i,
                    TeamID = GetTeamID(t),
                    Flags = PackFlags(t),
                    HarvestPoints = GetHarvestPoints(t)
                };
            }

            _trackablesDirty = false;
        }

        public void DisposeNativeTrackables()
        {
            if (_nativeTrackables.IsCreated)
            {
                _nativeTrackables.Dispose();
                _lastValidCount = 0;
            }
        }

        // -----------------------------------------------------------------
        // HELPER METHODS
        // -----------------------------------------------------------------
        private int GetTeamID(IChunkTrackable t)
            => t is NonPlayerCharacter n ? (int)n.TeamID : 0;

        private byte PackFlags(IChunkTrackable t)
        {
            byte f = 0;
            if (t.IsAttackable) f |= 1;
            if (t is HarvestNode h && h.RuntimeState.GetHarvestPoints() > 0) f |= 2;
            if (t is Stockpile) f |= 4;
            return f;
        }

        private short GetHarvestPoints(IChunkTrackable t)
            => t is HarvestNode h ? (short)h.RuntimeState.GetHarvestPoints() : (short)0;

        // -----------------------------------------------------------------
        // CONSTRUCTOR
        // -----------------------------------------------------------------
        public Chunk(FChunkPosition chunkID, ChunkManager manager)
        {
            ChunkID = chunkID;
            _manager = manager;
            _context = manager.Context;

            float chunkSize = WorldConstants.CHUNK_SIZE;

            Vector2 chunkCorner = new Vector2(
                chunkID.X * chunkSize,
                chunkID.Y * chunkSize
            );

            Vector2 center = new Vector2(
                chunkCorner.x + chunkSize / 2,
                chunkCorner.y + chunkSize / 2
            );

            Bounds = new Bounds(new Vector3(center.x, 0, center.y), new Vector3(chunkSize, 1000, chunkSize));
        }

        // -----------------------------------------------------------------
        // REPLICATION
        // -----------------------------------------------------------------
        public void SetReplicator(ChunkReplicator replicator)
        {
            _replicator = replicator;
            _hasReplicator = true;
        }

        public void ClearReplicator()
        {
            _replicator = null;
            _hasReplicator = false;
        }

        public void IncrementReplicationRef() => ReplicationRefCount++;
        public void DecrementReplicationRef() => ReplicationRefCount = Mathf.Max(ReplicationRefCount - 1, 0);

        // -----------------------------------------------------------------
        // TRACKABLE MANAGEMENT
        // -----------------------------------------------------------------
        public void AddObject(IChunkTrackable objId)
        {
            _trackablesInChunk.Add(objId);
            MarkTrackablesDirty(); // REBUILD ON NEXT GET
        }

        public void RemoveObject(IChunkTrackable objId)
        {
            _trackablesInChunk.Remove(objId);
            MarkTrackablesDirty(); // REBUILD ON NEXT GET
        }

        // -----------------------------------------------------------------
        // NPC MANAGEMENT
        // -----------------------------------------------------------------
        public void AddNPC(NonPlayerCharacter npc)
        {
            _trackablesInChunk.Add(npc);
            _npcsInChunk.Add(npc);
            MarkTrackablesDirty(); // REBUILD ON NEXT GET
        }

        public void RemoveNPC(NonPlayerCharacter npc)
        {
            _trackablesInChunk.Remove(npc);
            _npcsInChunk.Remove(npc);
            MarkTrackablesDirty(); // REBUILD ON NEXT GET
        }

        // -----------------------------------------------------------------
        // PROP INITIALIZATION
        // -----------------------------------------------------------------
        public void InitializeRuntimeStates(PropMarkupData[] propMarkupDatas)
        {
            _propRuntimeStates = new PropRuntimeState[propMarkupDatas.Length];
            PropLoadStates = new FPropLoadState[propMarkupDatas.Length];

            for (int i = 0; i < propMarkupDatas.Length; i++)
            {
                var propMarkupData = propMarkupDatas[i];
                if (propMarkupData == null) continue;

                var propRuntimeState = new PropRuntimeState(
                    propMarkupData.guid,
                    this,
                    propMarkupData.position,
                    propMarkupData.rotation,
                    propMarkupData.scale,
                    propMarkupData.propDefinitionId);

                _propRuntimeStates[i] = propRuntimeState;
                PropLoadStates[i] = new FPropLoadState();
            }
        }

        public void AddInvasionSpawnPoint(InvasionSpawnPoint spawnPoint)
        {
            _invasionSpawnPoints.Add(spawnPoint);
        }

        public void AddOrUpdateDeltaState(PropRuntimeState propState)
        {
            _deltaPropStates[propState.index] = propState;
            _manager.DeltaChunks.Add(this);
        }

        public void DespawnProps()
        {
            for (int i = 0; i < PropLoadStates.Length; i++)
            {
                var loadState = PropLoadStates[i];

                if (loadState.LoadState == ELoadState.Loaded)
                    loadState.Prop.StartRecycle();

                loadState.LoadState = ELoadState.None;
                loadState.Prop = null;
                PropLoadStates[i] = loadState;
            }

            DisposeNativeTrackables(); // ADD THIS
            _trackablesDirty = true;   // Force rebuild next time
        }

        // -----------------------------------------------------------------
        // PROP RENDER & NETWORK
        // -----------------------------------------------------------------
        public void UpdatePropRuntimeState(PropRuntimeState runtimeState)
        {
            if (!_hasReplicator)
            {
                Debug.Log("No replicator " + ChunkID.X + ", " + ChunkID.Y);
                return;
            }

            ref FPropData propData = ref _replicator.GetPropData(runtimeState.index);
            if (propData.IsValid())
            {
                FPropData runtimeData = runtimeState.Data;
                if (!propData.IsPropDataEqual(ref runtimeData))
                {
                    AddOrUpdateDeltaState(runtimeState);
                    runtimeState.CopyData(ref propData);
                }
            }
        }

        public void ReplicatePropState(PropRuntimeState replicatedState)
        {
            AddOrUpdateDeltaState(replicatedState);

            if (!_hasReplicator) return;

            ref FPropData data = ref _replicator.GetPropData(replicatedState.index);
            FPropData currentData = replicatedState.Data;
            data.Copy(ref currentData);
        }

        public bool GetRenderState(bool hasAuthority, int index, out PropRuntimeState usedState)
        {
            var authorityState = _propRuntimeStates[index];
            UpdatePropRuntimeState(authorityState);
            usedState = authorityState;

            if (hasAuthority) return true;

            bool hasPredicted = _predictedStates.TryGetValue(index, out var predictedState);
            bool hasLocal = _localRuntimePropStates.TryGetValue(index, out var localState);

            if (!hasLocal)
            {
                _localRuntimePropStates[index] = new PropRuntimeState(authorityState);
                return true;
            }

            if (localState.Data.StateData != authorityState.Data.StateData)
            {
                if (hasPredicted) _predictedStates.Remove(index);
                FPropData data = authorityState.Data;
                localState.CopyData(ref data);
                _localRuntimePropStates[index] = localState;
            }

            if (hasPredicted) usedState = predictedState;

            return true;
        }

        public void OnRender(bool hasAuthority, float renderDeltaTime)
        {

            _rebuildFrameCounter++;
            if (_rebuildFrameCounter >= REBUILD_EVERY_FRAMES)
            {
                RebuildNativeTrackables();
                _rebuildFrameCounter = 0;
            }

            for (int i = 0; i < _propRuntimeStates.Length; i++)
            {
                if (!GetRenderState(hasAuthority, i, out var usedState))
                {
                    Debug.LogWarning($"Null PropRuntimeState for GUID {i} in Render.");
                    continue;
                }

                var propLoadState = PropLoadStates[i];

                switch (propLoadState.LoadState)
                {
                    case ELoadState.None:
                        propLoadState.LoadState = ELoadState.Loading;
                        PropLoadStates[i] = propLoadState;
                        _context.PropManager.SpawnProp(usedState);
                        break;
                    case ELoadState.Loaded:
                        propLoadState.Prop.OnRender(usedState, renderDeltaTime);
                        break;
                }
            }
        }

        private readonly List<PropRuntimeState> _tempPropStateList = new List<PropRuntimeState>();

        public void OnFixedUpdateNetwork(int tick)
        {
            _tempPropStateList.Clear();
            foreach (var kvp in _deltaPropStates)
                _tempPropStateList.Add(kvp.Value);

            foreach (var propState in _tempPropStateList)
                if (propState.AuthorityUpdate(tick))
                    ReplicatePropState(propState);
        }
    }
}