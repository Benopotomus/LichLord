using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace LichLord.Props
{
    public class PropManager : ContextBehaviour
    {
        [SerializeField] private PropSpawner _propSpawner;

        [SerializeField] private LevelPropsMarkupData levelPropsMarkupData;
        [SerializeField] private PropReplicationData propReplicationPrefab; 
        [SerializeField] private string saveFileName = "PropSaveData.json"; // Save file name
        [SerializeField] private float spawnRadius = 50f; // Distance to spawn props
        [SerializeField] private float despawnRadius = 60f; // Distance to despawn props

        public List<PropRuntimeState> RuntimePropStates = new List<PropRuntimeState>(); // index is GUID
        private List<PropRuntimeState> deltaStates = new List<PropRuntimeState>(); // States that are different from base loaded defaults

        private List<PropLoadState> propLoadStates = new List<PropLoadState>(); 
        private List<PropReplicationData> propReplicators;
        private string saveFilePath;

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

                RuntimePropStates.Add(propRuntimeState);
                propLoadStates.Add(new PropLoadState());
            }
        }
        
        private void ApplySavedDelta()
        {
            // Load runtime state from file
            LoadSavedPropStates();

            for (int i = 0; i < deltaStates.Count; i++)
            {
                PropRuntimeState deltaState = deltaStates[i];
                int guid = deltaStates[i].guid;

                //get a state and overwrite it
                PropRuntimeState changedState = RuntimePropStates[guid];
                changedState.position = deltaState.position;
                changedState.rotation = deltaState.rotation;
                changedState.definitionID = deltaState.definitionID;
                changedState.data = deltaState.data;
            }

            int propReplicatorCount = (deltaStates.Count / PropConstants.MAX_PROP_REPS_NETOBJECT) + 1;

            for (int i = 0; i < propReplicatorCount; i++)
            {
                var propReplicationObject = Runner.Spawn(propReplicationPrefab, Vector3.zero, Quaternion.identity);

                propReplicators.Add(propReplicationObject);
                
                int startIndex = i * PropConstants.MAX_PROP_REPS_NETOBJECT;
                int endIndex = Mathf.Min(startIndex + PropConstants.MAX_PROP_REPS_NETOBJECT, RuntimePropStates.Count);

                for (int j = startIndex; j < endIndex; j++)
                {
                    propReplicationObject.AddProp(RuntimePropStates[j]);
                }
            }
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

            for (int i = 0; i < RuntimePropStates.Count; i++)
            {
                PropRuntimeState propState = RuntimePropStates[i];
                PropLoadState propLoadState = propLoadStates[i];

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
                else if (!shouldBeActive && distance > despawnRadius 
                    && propLoadState.LoadState == ELoadState.Loaded)
                {
                    DespawnProp(propState.guid);
                }
            }
        }

        private void OnPropSpawned(PropRuntimeState propRuntimeState, Prop prop)
        {
            int guid = propRuntimeState.guid;

            PropLoadState propLoadState = propLoadStates[guid];
            propLoadState.Prop = prop;
            propLoadState.LoadState = ELoadState.Loaded;
        }

        private void DespawnProp(int guid)
        {
            PropLoadState propLoadState = propLoadStates[guid];
            if (propLoadState.LoadState == ELoadState.Loaded)
            {
                propLoadState.Prop.StartRecycle();
                propLoadState.LoadState = ELoadState.None;
            }
        }

        private void LoadSavedPropStates()
        {
            deltaStates.Clear();

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
                            while (RuntimePropStates.Count <= guid)
                            {
                                RuntimePropStates.Add(null);
                                propLoadStates.Add(new PropLoadState());
                            }

                            // Update or add runtime state
                            RuntimePropStates[guid] = new PropRuntimeState(
                                guid,
                                entry.position,
                                entry.rotation,
                                entry.definitionId,
                                entry.data);

                            // Add to deltaStates for chunk initialization
                            deltaStates.Add(RuntimePropStates[guid]);
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
                for (int i = 0; i < RuntimePropStates.Count; i++)
                {
                    PropRuntimeState state = RuntimePropStates[i];
                    if (state != null) // Skip null entries
                    {
                        entries.Add(new PropSaveState(
                            state.guid,
                            state.position,
                            state.rotation,
                            state.definitionID,
                            state.data
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

        public void ResetRuntimeState()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    File.Delete(saveFilePath);
                    RuntimePropStates.Clear();
                    foreach (var loadState in propLoadStates)
                    {
                        if (loadState.LoadState == ELoadState.Loaded)
                        {
                            loadState.Prop.StartRecycle();
                        }
                    }
                    propLoadStates.Clear();
                    Debug.Log("Runtime state reset to default LevelPropsMarkupData.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to reset runtime state: {e.Message}");
                }
            }
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
    }
}