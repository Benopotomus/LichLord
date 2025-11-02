using UnityEngine;
using Fusion;
using System.Collections.Generic;
using LichLord.Props;

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

        private List<IHitTarget> _hitTargets = new List<IHitTarget>();
        public List<IHitTarget> HitTargets => _hitTargets;

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

        // Total number of players that should be replicating this
        public int ReplicationRefCount { get; private set; } = 0;

        // Prediction and reconcilation
        [SerializeField] private Dictionary<int, PropRuntimeState> _localRuntimePropStates = new Dictionary<int, PropRuntimeState>();
        [SerializeField] private Dictionary<int, PropRuntimeState> _predictedStates = new Dictionary<int, PropRuntimeState>();

        // Saved data
        private Dictionary<int, PropRuntimeState> _loadedPropStates = new Dictionary<int, PropRuntimeState>();
        public Dictionary<int, PropRuntimeState> LoadedPropStates => _loadedPropStates;

        public Chunk(FChunkPosition chunkID, ChunkManager manager)
        {
            ChunkID = chunkID;
            _manager = manager;
            _context = manager.Context;

            float chunkSize = WorldConstants.CHUNK_SIZE;

            // Calculate the chunk's world position, accounting for worldOrigin as the center
            Vector2 chunkCorner = new Vector2(
                chunkID.X * chunkSize,
                chunkID.Y * chunkSize
            );

            // Set Bounds center at the middle of the chunk
            Vector2 center = new Vector2(
                chunkCorner.x + chunkSize / 2,
                chunkCorner.y + chunkSize / 2
            );

            // Create Bounds with height 1000 (as in original) for 3D space
            Bounds = new Bounds(new Vector3(center.x, 0, center.y), new Vector3(chunkSize, 1000, chunkSize));
        }

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

        public void IncrementReplicationRef()
        {
            ReplicationRefCount++;
        }

        public void DecrementReplicationRef()
        {
            ReplicationRefCount = Mathf.Max(ReplicationRefCount - 1, 0);
        }

        public void AddObject(IChunkTrackable objId)
        {
            _trackablesInChunk.Add(objId);
        }

        public void RemoveObject(IChunkTrackable objId)
        {
            _trackablesInChunk.Remove(objId);
        }

        public void AddHitTarget(IHitTarget objId)
        {
            _hitTargets.Add(objId);
        }

        public void RemoveHitTarget(IHitTarget objId)
        {
            _hitTargets.Remove(objId);
        }

        public void InitializeRuntimeStates(PropMarkupData[] propMarkupDatas)
        {
            _propRuntimeStates = new PropRuntimeState[propMarkupDatas.Length];
            PropLoadStates = new FPropLoadState[propMarkupDatas.Length];

            for (int i = 0; i < propMarkupDatas.Length; i++)
            {
                PropMarkupData propMarkupData = propMarkupDatas[i];
                if (propMarkupData == null)
                {
                    continue;
                }

                PropRuntimeState propRuntimeState = new PropRuntimeState(
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

                if(loadState.LoadState == ELoadState.Loaded)
                    loadState.Prop.StartRecycle();

                loadState.LoadState = ELoadState.None;
                loadState.Prop = null;
                PropLoadStates[i] = loadState; // Explicitly write back the modified value
            }
        }

        public void UpdatePropRuntimeState(PropRuntimeState runtimeState)
        {
            if (!_hasReplicator)
            {
                Debug.Log("No replicator " + ChunkID.X + ", " + ChunkID.Y);
                return;
            }

            // if we have replication data, use that
            ref FPropData propData = ref _replicator.GetPropData(runtimeState.index);
            if (propData.IsValid())
            {
                FPropData runtimeData = runtimeState.Data;

                // if the data is not equal, we update the runtime state
                if (!propData.IsPropDataEqual(ref runtimeData))
                {
                    AddOrUpdateDeltaState(runtimeState);
                    runtimeState.CopyData(ref propData);
                }
            }
        }

        public void ReplicatePropState(PropRuntimeState replictedState)
        {
            AddOrUpdateDeltaState(replictedState);

            if (!_hasReplicator)
                return;

            ref FPropData data = ref _replicator.GetPropData(replictedState.index);
            FPropData currentData = replictedState.Data;
            data.Copy(ref currentData);
        }
         
        public bool GetRenderState(bool hasAuthority, int index, out PropRuntimeState usedState)
        {
            PropRuntimeState authorityState = _propRuntimeStates[index];

            // Update the authority state if there's a replicated value    
            UpdatePropRuntimeState(authorityState);

            usedState = authorityState;

            // If we are the authority, we dont need to handle prediction
            if (hasAuthority)
                return true;

            // Check for predicted data
            bool hasPredictedState = _predictedStates.TryGetValue(index, out PropRuntimeState predictedState);
            bool hasLocalState = _localRuntimePropStates.TryGetValue(index, out PropRuntimeState localState);

            // If there's no local state for this guid, create one and return
            if (!hasLocalState)
            {
                _localRuntimePropStates[index] = new PropRuntimeState(authorityState);
                return true;
            }

            // Check for if local data has changed
            if (localState.Data.StateData != authorityState.Data.StateData)
            {
                // remove predicted states for any changes
                if (hasPredictedState)
                {
                    hasPredictedState = false;
                    _predictedStates.Remove(index);
                }

                // update the local state data
                FPropData authorityData = authorityState.Data;
                localState.CopyData(ref authorityData);
                _localRuntimePropStates[index] = localState;
            }

            // if we still have a predicted state after checking authority changes
            // use that state
            if (hasPredictedState)
            {
                // Debug.Log("Using Predicted State");
                usedState = predictedState;
            }

            return true;
        }

        public void OnRender(bool hasAuthority, float renderDeltaTime)
        {
            for (int i = 0; i < _propRuntimeStates.Length; i++)
            {
                if (!GetRenderState(hasAuthority, i, out var usedState))
                {
                    Debug.LogWarning($"Null PropRuntimeState for GUID {i} in Render.");
                    continue;
                }

                FPropLoadState propLoadState =  PropLoadStates[i];

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
            {
                _tempPropStateList.Add(kvp.Value);
            }

            foreach (var propState in _tempPropStateList)
            {
                if (propState.AuthorityUpdate(tick))
                    ReplicatePropState(propState);
            }
        }
    }
}