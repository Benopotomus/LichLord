using Fusion;
using UnityEngine;
using System.Collections.Generic;
using LichLord.World;

namespace LichLord.Props
{
    public partial class PropManager : ContextBehaviour
    {
        [SerializeField] private PropSpawner _propSpawner;
        [SerializeField] private PropReplicator propReplicationPrefab;

        [SerializeField] private Dictionary<int, PropRuntimeState> _authorityRuntimePropStates = new Dictionary<int, PropRuntimeState>();
        [SerializeField] private List<PropReplicator> _propReplicators = new List<PropReplicator>();

        private Dictionary<int, PropLoadState> _propLoadStates = new Dictionary<int, PropLoadState>();
        private HashSet<FChunkPosition> _loadedChunks = new HashSet<FChunkPosition>(); // Track loaded chunks
        private HashSet<int> _usedGuids = new HashSet<int>(); // Track GUIDs to detect duplicates

        public override void Spawned()
        {
            _propSpawner.OnPropSpawned += OnPropSpawned;
        }

        public void LoadPropsForChunk(Chunk chunk)
        {
            if (chunk == null)
            {
                return;
            }

            ChunkPropsMarkupData baseMarkupData = Context.ChunkManager.WorldSettings.GetMarkupData(chunk.ChunkID);

            if (baseMarkupData == null)
                return;

            for (int i = 0; i < baseMarkupData.propMarkupDatas.Length; i++)
            {
                PropMarkupData propMarkupData = baseMarkupData.propMarkupDatas[i];
                if (propMarkupData == null || propMarkupData.propDefinition == null)
                {
                    continue;
                }

                if (propMarkupData.guid == 0 || _usedGuids.Contains(propMarkupData.guid))
                {
                    continue;
                }

                // if the authority state doesnt exist for this yet. we need ot add it
                if(!_authorityRuntimePropStates.TryGetValue(propMarkupData.guid, out var state))
                { 
                    PropRuntimeState propRuntimeState = new PropRuntimeState(
                        propMarkupData.guid,
                        propMarkupData.position,
                        propMarkupData.rotation,
                        propMarkupData.propDefinition.TableID);

                    SetupRuntimePropState(chunk, propRuntimeState);
                }
            }

            _loadedChunks.Add(chunk.ChunkID);
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

                // if we have no loader for guid, create one
                if (!_propLoadStates.TryGetValue(guid, out PropLoadState propLoadState))
                {
                    propLoadState = new PropLoadState();
                    _propLoadStates[guid] = propLoadState;
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
                     propLoadState.Prop.OnRender(usedState, renderDeltaTime);
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
            if (!Runner.IsFirstTick || !Runner.IsForward)
                return;

            float deltaTime = Runner.DeltaTime;

            EnsureEmptyReplicator();

            // Update authority data for things like hitreacts expiring
            // Create a snapshot to safely iterate
            var snapshot = new Dictionary<int, PropRuntimeState>(_authorityRuntimePropStates);

            foreach (var propState in snapshot)
            {
                PropRuntimeState runtimeState = propState.Value;

                if (runtimeState.AuthorityUpdate(deltaTime))
                {
                    ReplicateAuthorityData(runtimeState);
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
        }

        // Called from Spawned in a replicator
        public void AddReplicatorCallback(PropReplicator replicator)
        {
            if (replicator == null)
            {
                return;
            }
            _propReplicators.Add(replicator);
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