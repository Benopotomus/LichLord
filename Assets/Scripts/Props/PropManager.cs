using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using FusionHelpers;
using System;

namespace LichLord.Props
{
    public class PropManager : ContextBehaviour
    {
        [SerializeField] private PropSpawner _propSpawner;
        [SerializeField] private LevelPropsMarkupData levelPropsMarkupData;
        [SerializeField] private PropReplicationData propReplicationPrefab;
        [SerializeField] private string saveFileName = "PropSaveData.json";
        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private float despawnRadius = 60f;

        [SerializeField] private List<PropRuntimeState> _runtimePropStates = new List<PropRuntimeState>(); // index is GUID
        [SerializeField] private Dictionary<int, PropRuntimeState> _deltaStates = new Dictionary<int, PropRuntimeState>(); // Changed to Dictionary with guid as key

        private List<PropLoadState> _propLoadStates = new List<PropLoadState>();
        private List<PropReplicationData> propReplicators;
        private string saveFilePath;

        [SerializeField] private TickAlignedEventRelay _eventStub;

        public override void Spawned()
        {
            _propSpawner.OnPropSpawned += OnPropSpawned;
            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
            propReplicators = new List<PropReplicationData>();

            LoadBaseLevelProps();

            if (HasStateAuthority)
            {
                ApplySavedDelta();
            }

            RegisterEventListener((DamageEvent evt) => ApplyDamage(evt.guid, evt.impulse, evt.damage));
        }

        public void ApplyDamage(int guid, Vector3 impulse, int damage)
        {
            Debug.Log("ApplyDamage: " + guid);

            PropRuntimeState propRuntimeState = _runtimePropStates[guid];
            propRuntimeState.stateData = 1;

            // find its FPropData if it exists and adjust that
            List<PropReplicationData> repDatas = Runner.GetAllBehaviours<PropReplicationData>();

            bool dataFound = false;
            for (int i = 0; i < repDatas.Count; i++)
            {
                PropReplicationData repData = repDatas[i];

                if (repData.TryGetPropData(guid, out var propData))
                {
                    propData.StateData = propRuntimeState.stateData;
                    repData.UpdatePropData(guid, propData);
                    OverrideRuntimeData(ref propData);
                    dataFound = true;
                }
            }

            // if it doesnt exist, add a propdata for it
            if (!dataFound)
            {
                for (int i = 0; i < repDatas.Count; i++)
                {
                    PropReplicationData repData = repDatas[i];

                    if (repData.HasFreeProp())
                    {
                        // Adding a prop overrides the runtime data
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
        }

        protected void RegisterEventListener<T>(Action<T> listener) where T : unmanaged, INetworkEvent
        {
            _eventStub.RegisterEventListener(listener);
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

            // Initialize runtime states for all props
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
            // Load runtime state from file
            LoadSavedPropStates();

            foreach (PropRuntimeState deltaState in _deltaStates.Values)
            {
                int guid = deltaState.guid;

                // Ensure lists have enough capacity
                while (_runtimePropStates.Count <= guid)
                {
                    _runtimePropStates.Add(null);
                    _propLoadStates.Add(new PropLoadState());
                }

                // Update runtime state
                PropRuntimeState changedState = _runtimePropStates[guid];
                changedState.position = deltaState.position;
                changedState.rotation = deltaState.rotation;
                changedState.definitionId = deltaState.definitionId;
                changedState.stateData = deltaState.stateData;
            }

            int propReplicatorCount = (_deltaStates.Count + PropConstants.MAX_PROP_REPS_NETOBJECT - 1) / PropConstants.MAX_PROP_REPS_NETOBJECT;

            for (int i = 0; i < propReplicatorCount; i++)
            {
                var propReplicationObject = Runner.Spawn(propReplicationPrefab, Vector3.zero, Quaternion.identity);
                propReplicators.Add(propReplicationObject);

                int startIndex = i * PropConstants.MAX_PROP_REPS_NETOBJECT;
                int endIndex = Mathf.Min(startIndex + PropConstants.MAX_PROP_REPS_NETOBJECT, _deltaStates.Count);

                int index = 0;
                foreach (PropRuntimeState deltaState in _deltaStates.Values)
                {
                    if (index >= startIndex && index < endIndex)
                    {
                        propReplicationObject.AddProp(deltaState);
                    }
                    index++;
                }
            }

            // Add another prop replicator so we have one at least to start with.
            Runner.Spawn(propReplicationPrefab, Vector3.zero, Quaternion.identity);
        }

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            PlayerCreature.TryGetLocalPlayer(Runner, out PlayerCreature playerCreature);

            if (playerCreature == null)
                return;

            Vector3 viewPosition = playerCreature.transform.position;
            float renderDeltaTime = Runner.LocalAlpha;

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
                    propLoadState.Prop.UpdateProp(propState, renderDeltaTime);
                }
                else if (!shouldBeActive && distance > despawnRadius && propLoadState.LoadState == ELoadState.Loaded)
                {
                    DespawnProp(propState.guid);
                }
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

        public void OverrideRuntimeData(ref FPropData data)
        {
            // Access PropManager
            PropManager propManager = Context.PropManager;
            if (propManager == null)
            {
                Debug.LogError("PropManager not found in Context.", this);
                return;
            }

            // Ensure RuntimePropStates and propLoadStates have enough capacity
            while (_runtimePropStates.Count <= data.GUID)
            {
                PropRuntimeState addedState = new PropRuntimeState(
                    _runtimePropStates.Count,
                    Vector3.zero,
                    Quaternion.identity,
                    0,
                    0);

                // Add to the runtime list and loadstate list
                _runtimePropStates.Add(addedState);
                _propLoadStates.Add(new PropLoadState());
            }

            PropRuntimeState modifiedState = _runtimePropStates[data.GUID];

            if (!data.IsEqualToRuntimeData(modifiedState))
            {
                modifiedState.guid = data.GUID;
                modifiedState.position = data.Position;
                modifiedState.rotation = Quaternion.LookRotation(data.Forward, Vector3.up);
                modifiedState.definitionId = data.DefinitionID;
                modifiedState.stateData = data.StateData;
            }

            _deltaStates[data.GUID] = modifiedState;
        }


        private void LoadSavedPropStates()
        {
            _deltaStates.Clear();

            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    PropSaveData saveData = JsonUtility.FromJson<PropSaveData>(json);
                    if (saveData?.props != null)
                    {
                        foreach (var entry in saveData.props)
                        {
                            int guid = entry.guid;

                            // Ensure lists have enough capacity
                            while (_runtimePropStates.Count <= guid)
                            {
                                _runtimePropStates.Add(null);
                                _propLoadStates.Add(new PropLoadState());
                            }

                            // Create runtime state
                            PropRuntimeState runtimeState = new PropRuntimeState(
                                guid,
                                entry.position,
                                entry.rotation,
                                entry.definitionId,
                                entry.stateData);

                            // Update RuntimePropStates
                            _runtimePropStates[guid] = runtimeState;

                            // Add to deltaStates with guid as key
                            _deltaStates[guid] = runtimeState;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load saved state from {saveFilePath}: {e.Message}");
                }
            }
        }

        private void SaveRuntimeState()
        {
            try
            {
                var entries = new List<PropSaveState>();
                foreach (PropRuntimeState state in _deltaStates.Values)
                {
                    if (state != null) // Skip null entries
                    {
                        entries.Add(new PropSaveState(
                            state.guid,
                            state.position,
                            state.rotation,
                            state.definitionId,
                            state.stateData
                        ));
                    }
                }

                PropSaveData saveData = new PropSaveData { props = entries.ToArray() };
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(saveFilePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save runtime state to {saveFilePath}: {e.Message}");
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            SaveRuntimeState();
        }

        public class PropLoadState
        {
            public Prop Prop;
            public ELoadState LoadState;
        }

        public enum ELoadState
        {
            None,
            Loading,
            Loaded,
            Unloading,
        }

        public struct DamageEvent : INetworkEvent
        {
            public int guid;
            public Vector3 impulse;
            public int damage;
        }
    }
}