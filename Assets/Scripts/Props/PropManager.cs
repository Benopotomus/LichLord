using Fusion;
using UnityEngine;
using System.Collections.Generic;
using FusionHelpers;

namespace LichLord.Props
{
    public class PropManager : ContextBehaviour
    {
        [SerializeField] private PropSpawner _propSpawner;
        [SerializeField] private LevelPropsMarkupData levelPropsMarkupData;
        [SerializeField] private PropReplicator propReplicationPrefab;
        [SerializeField] private PropSaveLoadManager saveLoadManager;
        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private float despawnRadius = 60f;

        [SerializeField] private List<PropRuntimeState> _runtimePropStates = new List<PropRuntimeState>();
        [SerializeField] private Dictionary<int, PropRuntimeState> _deltaStates = new Dictionary<int, PropRuntimeState>();
        [SerializeField] private List<PropReplicator> _propReplicators;

        private List<PropLoadState> _propLoadStates = new List<PropLoadState>();

        public override void Spawned()
        {
            _propSpawner.OnPropSpawned += OnPropSpawned;
            _propReplicators = new List<PropReplicator>();

            LoadBaseLevelProps();

            if (HasStateAuthority)
            {
                ApplySavedDelta();
            }
        }

        public void AddReplicator(PropReplicator replicationData)
        {
            _propReplicators.Add(replicationData);
        }

        public void ApplyDamage(int guid, Vector3 impulse, int damage)
        {
            Debug.Log("ApplyDamage: " + guid);

            PropRuntimeState propRuntimeState = _runtimePropStates[guid];
            propRuntimeState.stateData = 1;

            bool dataFound = false;
            for (int i = 0; i < _propReplicators.Count; i++)
            {
                PropReplicator repData = _propReplicators[i];

                if (repData.TryGetPropData(guid, out var propData))
                {
                    propData.StateData = propRuntimeState.stateData;
                    repData.UpdatePropData(guid, propData);
                    dataFound = true;
                }
            }

            if (!dataFound)
            {
                for (int i = 0; i < _propReplicators.Count; i++)
                {
                    PropReplicator repData = _propReplicators[i];

                    if (repData.HasFreeProp())
                    {
                        repData.AddProp(propRuntimeState);
                        break;
                    }
                }
            }

            PropLoadState loadState = _propLoadStates[guid];
            if (loadState.LoadState == ELoadState.Loaded)
            {
                loadState.Prop.UpdateProp(propRuntimeState, Runner.LocalAlpha);
            }

            // Update delta states for saving
            _deltaStates[guid] = propRuntimeState;
        }

        private void LoadBaseLevelProps()
        {
            if (levelPropsMarkupData == null)
            {
                Debug.LogError("PropPointMarkupData is not assigned.", this);
                return;
            }

            if (levelPropsMarkupData.propMarkupDatas == null || levelPropsMarkupData.propMarkupDatas.Length == 0)
            {
                Debug.LogWarning("PropPointMarkupData has no prop points.", this);
                return;
            }

            for (int i = 0; i < levelPropsMarkupData.propMarkupDatas.Length; i++)
            {
                PropMarkupData propMarkupData = levelPropsMarkupData.propMarkupDatas[i];

                if (propMarkupData.propDefinition == null)
                {
                    Debug.LogWarning($"Skipping invalid prop point with guid {i}.", this);
                    continue;
                }

                PropRuntimeState propRuntimeState = new PropRuntimeState(
                    i,
                    propMarkupData.position,
                    propMarkupData.rotation,
                    propMarkupData.propDefinition.TableID,
                    0);

                _runtimePropStates.Add(propRuntimeState);
                _propLoadStates.Add(new PropLoadState());
            }
        }

        private void ApplySavedDelta()
        {
            saveLoadManager.LoadSavedPropStates(_runtimePropStates, _propLoadStates, _deltaStates);

            foreach (PropRuntimeState deltaState in _deltaStates.Values)
            {
                int guid = deltaState.guid;
                PropRuntimeState changedState = _runtimePropStates[guid];
                changedState.position = deltaState.position;
                changedState.rotation = deltaState.rotation;
                changedState.definitionId = deltaState.definitionId;
                changedState.stateData = deltaState.stateData;
            }

            int propReplicatorCount = (_deltaStates.Count + PropConstants.MAX_PROP_REPS - 1) / PropConstants.MAX_PROP_REPS;

            for (int i = 0; i < propReplicatorCount; i++)
            {
                var propReplicationObject = Runner.Spawn(propReplicationPrefab, Vector3.zero, Quaternion.identity);
                _propReplicators.Add(propReplicationObject);

                int startIndex = i * PropConstants.MAX_PROP_REPS;
                int endIndex = Mathf.Min(startIndex + PropConstants.MAX_PROP_REPS, _deltaStates.Count);

                int index = 0;
                foreach (PropRuntimeState deltaState in _deltaStates.Values)
                {
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

            Vector3 viewPosition = playerCreature.transform.position;
            float renderDeltaTime = Runner.LocalAlpha;

            // Ensure an empty replicator exists on the master client
            if (Runner.IsSharedModeMasterClient)
            {
                EnsureEmptyReplicator();
            }

            for (int i = 0; i < _runtimePropStates.Count; i++)
            {
                PropRuntimeState propState = _runtimePropStates[i];
                PropLoadState propLoadState = _propLoadStates[i];

                float distance = Vector3.Distance(viewPosition, propState.position);
                bool shouldBeActive = distance <= spawnRadius;

                if (shouldBeActive && propLoadState.LoadState == ELoadState.None)
                {
                    propLoadState.LoadState = ELoadState.Loading;
                    _propSpawner.SpawnProp(propState);
                }
                else if (shouldBeActive && propLoadState.LoadState == ELoadState.Loaded)
                {
                    RefreshRuntimeState(propState);
                    propLoadState.Prop.UpdateProp(propState, renderDeltaTime);
                }
                else if (!shouldBeActive && distance > despawnRadius && propLoadState.LoadState == ELoadState.Loaded)
                {
                    DespawnProp(propState.guid);
                }
            }
        }

        private void EnsureEmptyReplicator()
        {
            // Check if there is at least one completely empty replicator (zero entries)
            bool hasEmptyReplicator = false;
            foreach (var replicator in _propReplicators)
            {
                if (replicator.DataCount == 0)
                {
                    hasEmptyReplicator = true;
                    break;
                }
            }

            // If no empty replicator exists, spawn a new one
            if (!hasEmptyReplicator)
            {
                var newReplicator = Runner.Spawn(propReplicationPrefab, Vector3.zero, Quaternion.identity);
            }
        }

        private void RefreshRuntimeState(PropRuntimeState propState)
        {
            bool replicatorFound = false;

            for (int i = 0; i < _propReplicators.Count; i++)
            {
                if (_propReplicators[i].TryGetPropData(propState.guid, out FPropData propData))
                {
                    replicatorFound = true;
                    propState.guid = propData.GUID;
                    propState.stateData = propData.StateData;
                }
            }

            // if no replicator, reset default state
            if (!replicatorFound)
            {
                propState.stateData = 0;
            }
        }

        private void OnPropSpawned(PropRuntimeState propRuntimeState, Prop prop)
        {
            int guid = propRuntimeState.guid;
            PropLoadState propLoadState = _propLoadStates[guid];
            propLoadState.Prop = prop;
            propLoadState.LoadState = ELoadState.Loaded;
            prop.OnSpawned(propRuntimeState, this);
        }

        private void DespawnProp(int guid)
        {
            PropLoadState propLoadState = _propLoadStates[guid];
            if (propLoadState.LoadState == ELoadState.Loaded)
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

        public class PropLoadState
        {
            public Prop Prop;
            public ELoadState LoadState;
        }
    }
}