using Fusion;
using UnityEngine;
using System.Collections.Generic;
using LichLord.World;

namespace LichLord.Props
{
    public partial class PropManager : ContextBehaviour
    {
        [SerializeField] private PropSpawner _propSpawner;
        [SerializeField] private WorldSettings worldSettings;
        [SerializeField] private PropReplicator propReplicationPrefab;
        [SerializeField] private PropSaveLoadManager saveLoadManager;

        [SerializeField] private Dictionary<int, PropRuntimeState> _authorityRuntimePropStates = new Dictionary<int, PropRuntimeState>();
        [SerializeField] private Dictionary<int, PropRuntimeState> _deltaStates = new Dictionary<int, PropRuntimeState>();
        [SerializeField] private List<PropReplicator> _propReplicators;

        private Dictionary<int, PropLoadState> _propLoadStates = new Dictionary<int, PropLoadState>();
        private HashSet<Vector2Int> _loadedChunks = new HashSet<Vector2Int>(); // Track loaded chunks
        private HashSet<int> _usedGuids = new HashSet<int>(); // Track GUIDs to detect duplicates

        public override void Spawned()
        {
            _propSpawner.OnPropSpawned += OnPropSpawned;
            _propReplicators = new List<PropReplicator>();

            Debug.Log($"PropManager Spawned. _runtimePropStates count: {_authorityRuntimePropStates.Count}, _propLoadStates count: {_propLoadStates.Count}");

            if (HasStateAuthority)
            {
               // ApplySavedDelta();
            }

            Debug.Log($"Post-cleanup _runtimePropStates count: {_authorityRuntimePropStates.Count}, _propLoadStates count: {_propLoadStates.Count}");
        }

        public void LoadPropsForChunk(Vector2Int chunkCoord)
        {
            ChunkPropsMarkupData markupData = worldSettings.PropMarkupDatas.Find(data => data != null && data.ChunkCoord == chunkCoord);
            if (markupData == null || markupData.propMarkupDatas == null || markupData.propMarkupDatas.Length == 0)
            {
                Debug.Log($"No props found for chunk {chunkCoord}.", this);
                _loadedChunks.Add(chunkCoord); // Mark as loaded
                return;
            }

            Vector3 firstPropPosition = markupData.propMarkupDatas[0]?.position ?? Vector3.zero;
            Chunk chunk = Context.ChunkManager.GetChunkAtPosition(firstPropPosition);
            if (chunk == null)
            {
                Debug.LogWarning($"No chunk found for markup data in chunk {chunkCoord} at position {firstPropPosition}.", this);
                return;
            }

            int validProps = 0;
            int initialStateCount = _authorityRuntimePropStates.Count;
            for (int i = 0; i < markupData.propMarkupDatas.Length; i++)
            {
                PropMarkupData propMarkupData = markupData.propMarkupDatas[i];
                if (propMarkupData == null)
                {
                    Debug.LogWarning($"Null PropMarkupData at index {i} in chunk {chunkCoord}.", this);
                    continue;
                }

                if (propMarkupData.propDefinition == null)
                {
                    Debug.LogWarning($"Skipping invalid prop point with GUID {propMarkupData.guid} in chunk {chunkCoord}: null propDefinition.", this);
                    continue;
                }

                if (propMarkupData.guid == 0 || _usedGuids.Contains(propMarkupData.guid))
                {
                    Debug.LogWarning($"Duplicate or invalid GUID {propMarkupData.guid} in chunk {chunkCoord}. Run 'Clean Up World Settings' in Level Editor.", this);
                    continue;
                }
                _usedGuids.Add(propMarkupData.guid);

                PropRuntimeState propRuntimeState = new PropRuntimeState(
                    propMarkupData.guid,
                    propMarkupData.position,
                    propMarkupData.rotation,
                    propMarkupData.propDefinition.TableID,
                    0);

                Debug.Log($"Loaded PropRuntimeState for GUID {propMarkupData.guid} in chunk {chunkCoord} at position {propMarkupData.position}.", this);

                _authorityRuntimePropStates.Add(propMarkupData.guid, propRuntimeState);
                _propLoadStates.Add(propMarkupData.guid, new PropLoadState());
                chunk.AddObject(propRuntimeState); // Add to chunk's PropStates
                validProps++;
            }

            _loadedChunks.Add(chunkCoord);
            Debug.Log($"Loaded {validProps} props for chunk {chunkCoord}. Total states: {_authorityRuntimePropStates.Count} (added {validProps} from {initialStateCount}).", this);

            // Apply delta states for newly loaded props
            if (HasStateAuthority)
            {
                foreach (var kvp in _authorityRuntimePropStates)
                {
                    PropRuntimeState state = kvp.Value;
                    if (state == null)
                    {
                        Debug.LogWarning($"Null PropRuntimeState for GUID {kvp.Key} after loading chunk {chunkCoord}.", this);
                        continue;
                    }

                    if (_deltaStates.TryGetValue(state.guid, out PropRuntimeState deltaState))
                    {
                        Debug.Log($"Applying delta state for GUID {state.guid} in chunk {chunkCoord}.", this);
                        state.position = deltaState.position;
                        state.rotation = deltaState.rotation;
                        state.definitionId = deltaState.definitionId;
                        state.stateData = deltaState.stateData;
                    }
                }
            }
        }

        public void AddReplicator(PropReplicator replicationData)
        {
            if (replicationData == null)
            {
                Debug.LogWarning("Null PropReplicator passed to AddReplicator.", this);
                return;
            }
            _propReplicators.Add(replicationData);
        }

        private void ApplySavedDelta()
        {
            Debug.Log($"Applying saved delta states. _deltaStates count: {_deltaStates.Count}, _runtimePropStates count: {_authorityRuntimePropStates.Count}");

            // saveLoadManager.LoadSavedPropStates(_runtimePropStates.Values, _propLoadStates.Values, _deltaStates);

            foreach (PropRuntimeState deltaState in _deltaStates.Values)
            {
                if (deltaState == null)
                {
                    Debug.LogWarning("Null deltaState in _deltaStates.", this);
                    continue;
                }

                if (!_authorityRuntimePropStates.TryGetValue(deltaState.guid, out PropRuntimeState changedState) || changedState == null)
                {
                    Debug.LogWarning($"Invalid or null PropRuntimeState for GUID {deltaState.guid} in ApplySavedDelta.", this);
                    continue;
                }

                changedState.position = deltaState.position;
                changedState.rotation = deltaState.rotation;
                changedState.definitionId = deltaState.definitionId;
                changedState.stateData = deltaState.stateData;
            }

            int propReplicatorCount = (_deltaStates.Count + PropConstants.MAX_PROP_REPS - 1) / PropConstants.MAX_PROP_REPS;

            for (int i = 0; i < propReplicatorCount; i++)
            {
                var propReplicationObject = Runner.Spawn(propReplicationPrefab, Vector3.zero, Quaternion.identity);
                if (propReplicationObject == null)
                {
                    Debug.LogWarning($"Failed to spawn PropReplicator {i}.", this);
                    continue;
                }
                _propReplicators.Add(propReplicationObject);

                int startIndex = i * PropConstants.MAX_PROP_REPS;
                int endIndex = Mathf.Min(startIndex + PropConstants.MAX_PROP_REPS, _deltaStates.Count);

                int index = 0;
                foreach (PropRuntimeState deltaState in _deltaStates.Values)
                {
                    if (deltaState == null)
                        continue;

                    if (index >= startIndex && index < endIndex)
                    {
                        propReplicationObject.AddProp(deltaState, true);
                    }
                    index++;
                }
            }

            Runner.Spawn(propReplicationPrefab, Vector3.zero, Quaternion.identity);
        }

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            PlayerCharacter.TryGetLocalPlayer(Runner, out PlayerCharacter playerCreature);

            if (playerCreature == null)
                return;

            float renderDeltaTime = Runner.LocalAlpha;
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            // Ensure an empty replicator exists on the master client
            if (Runner.IsSharedModeMasterClient)
            {
                EnsureEmptyReplicator();
            }

            //Debug.Log($"Rendering with _runtimePropStates count: {playerCreature.CachedPropStates.Count}");

            // Update states from player's cached list
            foreach (PropRuntimeState propState in playerCreature.CachedPropStates)
            {
                int guid = propState.guid;

                if(!GetRenderState(hasAuthority, guid, out var usedState))
                {
                    Debug.LogWarning($"Null PropRuntimeState for GUID {guid} in Render.", this);
                    continue;
                }

                //Debug.Log(guid + " " + usedState.guid + " " + usedState.stateData);

                if (!_propLoadStates.TryGetValue(guid, out PropLoadState propLoadState))
                {
                    Debug.LogWarning($"Null or missing PropLoadState for GUID {guid} in Render.", this);
                    continue;
                }

                // Get the chunk for this prop's position
                Chunk propChunk = Context.ChunkManager.GetChunkAtPosition(usedState.position);
                if (propChunk == null)
                {
                    Debug.LogWarning($"No chunk found for prop at position {usedState.position} (GUID {guid}).", this);
                    continue;
                }

                if (propLoadState.LoadState == ELoadState.None)
                {
                    propLoadState.LoadState = ELoadState.Loading;
                    _propSpawner.SpawnProp(usedState);
                }
                else if (propLoadState.LoadState == ELoadState.Loaded && propLoadState.Prop != null)
                {
                    propLoadState.Prop.UpdateProp(usedState, renderDeltaTime);
                }
                else if (propLoadState.LoadState == ELoadState.Loaded && propLoadState.Prop != null)
                {
                    DespawnProp(guid);
                }
            }
        }

        private void EnsureEmptyReplicator()
        {
            bool hasEmptyReplicator = false;
            foreach (var replicator in _propReplicators)
            {
                if (replicator == null)
                    continue;
                if (replicator.DataCount == 0)
                {
                    hasEmptyReplicator = true;
                    break;
                }
            }

            if (!hasEmptyReplicator)
            {
                var newReplicator = Runner.Spawn(propReplicationPrefab, Vector3.zero, Quaternion.identity);
                if (newReplicator != null)
                {
                    _propReplicators.Add(newReplicator);
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            float deltaTime = Runner.DeltaTime;

            EnsureEmptyReplicator();


            // Run through the loaded runtime states
            // and update it with authority
            foreach (var propState in _authorityRuntimePropStates)
            {
                PropRuntimeState runtimeState = propState.Value;
                if (runtimeState.UpdateState(deltaTime))
                { 
                    //ReplicateStateChange(runtimeState);
                }
            }
        }

        private void OnPropSpawned(PropRuntimeState propRuntimeState, Prop prop)
        {
            if (propRuntimeState == null)
            {
                Debug.LogWarning("Null propRuntimeState in OnPropSpawned.", this);
                return;
            }

            int guid = propRuntimeState.guid;
            if (!_propLoadStates.TryGetValue(guid, out PropLoadState propLoadState) || propLoadState == null)
            {
                Debug.LogWarning($"Missing or null PropLoadState for GUID {guid} in OnPropSpawned.", this);
                return;
            }

            propLoadState.Prop = prop;
            propLoadState.LoadState = ELoadState.Loaded;

            prop.OnSpawned(propRuntimeState, this);
        }

        public void DespawnProp(int guid)
        {
            if (!_propLoadStates.TryGetValue(guid, out PropLoadState propLoadState) || propLoadState == null)
            {
                Debug.LogWarning($"Missing or null PropLoadState for GUID {guid} in DespawnProp.", this);
                return;
            }

            if (propLoadState.LoadState == ELoadState.Loaded && propLoadState.Prop != null)
            {
                propLoadState.Prop.StartRecycle();
                propLoadState.LoadState = ELoadState.None;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            if (runner.IsSharedModeMasterClient)
            {
                saveLoadManager.SaveRuntimeState(_deltaStates);
            }
        }

        public PropReplicator GetReplicatorWithFreeSlots()
        {

            for (int i = 0; i < _propReplicators.Count; i++)
            {
                PropReplicator propReplicator = _propReplicators[i];
                if(propReplicator.HasFreeProp())
                    return propReplicator;
            }

            Debug.Log("No replicator with free slots found");
            return null;
        }

        public class PropLoadState
        {
            public Prop Prop;
            public ELoadState LoadState;
        }
    }
}