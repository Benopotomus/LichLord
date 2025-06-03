using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace LichLord.Buildables
{
    public class BuildableSaveLoadManager : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "BuildSaveData.json";
        private string saveFilePath;

        private void Awake()
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        }

        public void LoadSavedPropStates(List<BuildableRuntimeState> runtimePropStates, List<BuildableManager.BuildableLoadState> propLoadStates, Dictionary<int, BuildableRuntimeState> deltaStates)
        {
            /*
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

                            while (runtimePropStates.Count <= guid)
                            {
                                runtimePropStates.Add(null);
                                propLoadStates.Add(new PropManager.PropLoadState());
                            }

                            PropRuntimeState runtimeState = new PropRuntimeState(
                                guid,
                                entry.position,
                                entry.rotation,
                                entry.definitionId,
                                entry.stateData);

                            runtimePropStates[guid] = runtimeState;
                            deltaStates[guid] = runtimeState;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load saved state from {saveFilePath}: {e.Message}");
                }
            }
            */
        }

        public void SaveRuntimeState(Dictionary<int, BuildableRuntimeState> deltaStates)
        {
            /*
            try
            {
                var entries = new List<PropSaveState>();
                foreach (PropRuntimeState state in deltaStates.Values)
                {
                    if (state != null)
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
            catch (Exception e)
            {
                Debug.LogError($"Failed to save runtime state to {saveFilePath}: {e.Message}");
            }
            */
        }
    }
}