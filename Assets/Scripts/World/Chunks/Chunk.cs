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
        public ChunkReplicator Replicator;
    
        private List<IChunkTrackable> _trackablesInChunk = new List<IChunkTrackable>();
        public List<IChunkTrackable> Trackables => _trackablesInChunk;

        private List<InvasionSpawnPoint> _invasionSpawnPoints = new List<InvasionSpawnPoint>();
        public List<InvasionSpawnPoint> InvasionSpawnPoints => _invasionSpawnPoints;

        private Dictionary<int, PropRuntimeState> _propStates = new Dictionary<int, PropRuntimeState>();
        public Dictionary<int, PropRuntimeState> PropStates => _propStates;

        private Dictionary<int, PropRuntimeState> _deltaPropStates = new Dictionary<int, PropRuntimeState>();
        public Dictionary<int, PropRuntimeState> DeltaPropStates => _deltaPropStates;

        public FChunkPosition ChunkID { get; set; }
        public Bounds Bounds { get; set; }

        public ELoadState LoadState { get; set; }

        public Dictionary<int, FPropLoadState> PropLoadStates = new Dictionary<int, FPropLoadState>();

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
            if (!_trackablesInChunk.Contains(objId))
                _trackablesInChunk.Add(objId);
        }

        public void RemoveObject(IChunkTrackable objId)
        {
            _trackablesInChunk.Remove(objId);
        }

        public void AddPropRuntimeState(PropRuntimeState propState)
        {
            _propStates[propState.guid] = propState;
            PropLoadStates[propState.guid] = new FPropLoadState();
        }

        public void AddInvasionSpawnPoint(InvasionSpawnPoint spawnPoint)
        {
            _invasionSpawnPoints.Add(spawnPoint);
        }

        public void RemoveObject(PropRuntimeState propState)
        {
            _propStates.Remove(propState.guid);
        }

        public void AddOrUpdateDeltaState(PropRuntimeState propState)
        {
            _deltaPropStates[propState.guid] = propState;
            _manager.DeltaChunks.Add(this);
        }

        public void DespawnProps()
        {
            var keys = new List<int>(PropLoadStates.Keys); // Create a list to avoid modifying collection during enumeration

            foreach (var key in keys)
            {
                var loadState = PropLoadStates[key];

                if(loadState.LoadState == ELoadState.Loaded)
                    loadState.Prop.StartRecycle();

                loadState.LoadState = ELoadState.None;
                PropLoadStates[key] = loadState; // Explicitly write back the modified value
            }

            _predictedStates.Clear();
            _localRuntimePropStates.Clear();
        }

        public void UpdatePropRuntimeState(PropRuntimeState runtimeState)
        {
            if (Replicator == null)
            {
                Debug.Log("No replicator " + ChunkID.X + ", " + ChunkID.Y);
                return;
            }

            // if we have replication data, use that
            ref FPropData propData = ref Replicator.GetPropData(runtimeState.guid);
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
            ref FPropData data = ref Replicator.GetPropData(replictedState.guid);
            FPropData currentData = replictedState.Data;
            data.Copy(ref currentData);
        }
         
        public bool GetRenderState(bool hasAuthority, int guid, out PropRuntimeState usedState)
        {
            if (!PropStates.TryGetValue(guid, out PropRuntimeState authorityState))
            {
                usedState = null;
                Debug.Log("Get Render State: Trying to get runtime data but its not loaded " + guid);
                return false;
            }

            // Update the authority state if there's a replicated value    
            UpdatePropRuntimeState(authorityState);

            usedState = authorityState;

            // If we are the authority, we dont need to handle prediction
            if (hasAuthority)
                return true;

            // Check for predicted data
            bool hasPredictedState = _predictedStates.TryGetValue(guid, out PropRuntimeState predictedState);
            bool hasLocalState = _localRuntimePropStates.TryGetValue(guid, out PropRuntimeState localState);

            // If there's no local state for this guid, create one and return
            if (!hasLocalState)
            {
                _localRuntimePropStates[guid] = new PropRuntimeState(authorityState);
                return true;
            }

            // Check for if local data has changed
            if (localState.Data.StateData != authorityState.Data.StateData)
            {
                // remove predicted states for any changes
                if (hasPredictedState)
                {
                    hasPredictedState = false;
                    _predictedStates.Remove(guid);
                }

                // update the local state data
                FPropData authorityData = authorityState.Data;
                localState.CopyData(ref authorityData);
                _localRuntimePropStates[guid] = localState;
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
            foreach (var kvp in PropStates)
            {
                int guid = kvp.Key;

                if (!GetRenderState(hasAuthority, guid, out var usedState))
                {
                    Debug.LogWarning($"Null PropRuntimeState for GUID {guid} in Render.");
                    continue;
                }

                // if we have no loader for guid, create one
                if (!PropLoadStates.TryGetValue(guid, out FPropLoadState propLoadState))
                {
                    propLoadState = new FPropLoadState();
                    PropLoadStates[guid] = propLoadState;
                }

                if (propLoadState.LoadState == ELoadState.None)
                {
                    // Set the state to loading
                    propLoadState.LoadState = ELoadState.Loading;
                    PropLoadStates[guid] = propLoadState;

                    _context.PropManager.SpawnProp(usedState);
                }
                else if (propLoadState.LoadState == ELoadState.Loaded && propLoadState.Prop != null)
                {
                    propLoadState.Prop.OnRender(usedState, renderDeltaTime);
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