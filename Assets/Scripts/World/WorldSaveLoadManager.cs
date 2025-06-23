using UnityEngine;
using System.IO;
using Fusion;
using System.Collections.Generic;
using System;

namespace LichLord.World
{
    public class WorldSaveLoadManager : ContextBehaviour
    {
        private Dictionary<FChunkPosition, FChunkSaveData> _loadedChunks = new Dictionary<FChunkPosition, FChunkSaveData>();
        public Dictionary<FChunkPosition, FChunkSaveData> LoadedChunks => _loadedChunks;

        private void SaveChunks()
        {
            if (Runner == null || Runner.SessionInfo == null || string.IsNullOrEmpty(Runner.SessionInfo.Name))
            {
                Debug.LogWarning("No active session; cannot save chunks.");
                return;
            }

            try
            {
                string sessionName = Runner.SessionInfo.Name;
                string saveFilePath = GetSaveFilePath(sessionName);

                // Get all the world chunks
                var worldChunks = Context.ChunkManager.WorldChunks;

                // Hold onto a dictionary of chunk save datas
                Dictionary<FChunkPosition, FChunkSaveData> chunkSaveDatas = new Dictionary<FChunkPosition, FChunkSaveData>();
                int totalPropCount = 0; // Manual counter for props

                foreach (var chunkPair in worldChunks)
                {
                    FChunkPosition chunkCoord = chunkPair.Key;
                    Chunk chunk = chunkPair.Value;
                    if (chunk.DeltaPropStates == null || chunk.DeltaPropStates.Count == 0)
                    {
                        continue;
                    }

                    List<FPropSaveState> propList = new List<FPropSaveState>();
                    foreach (var deltaState in chunk.DeltaPropStates.Values)
                    {
                        if (deltaState == null)
                        {
                            Debug.LogWarning($"Null PropRuntimeState for GUID {deltaState?.guid} in chunk {chunkCoord.X}/{chunkCoord.Y}.");
                            continue;
                        }

                        propList.Add(new FPropSaveState(
                            deltaState.guid,
                            deltaState.position,
                            deltaState.rotation,
                            deltaState.definitionId,
                            deltaState.Data.StateData
                        ));
                    }

                    if (propList.Count > 0)
                    {
                        // Create or update FChunkSaveData
                        FChunkSaveData chunkSaveData;
                        if (!chunkSaveDatas.TryGetValue(chunkCoord, out chunkSaveData))
                        {
                            chunkSaveData = new FChunkSaveData { chunkCoord = chunkCoord };
                        }

                        // Update the props field
                        chunkSaveData.props = propList.ToArray();

                        // Reassign to dictionary to persist changes
                        chunkSaveDatas[chunkCoord] = chunkSaveData;
                        totalPropCount += propList.Count; // Increment prop counter
                    }
                }

                // Convert to WorldSaveData for serialization
                FWorldSaveData saveData = new FWorldSaveData
                {
                    chunks = new FChunkSaveData[chunkSaveDatas.Count]
                };
                int index = 0;
                foreach (var chunkData in chunkSaveDatas.Values)
                {
                    saveData.chunks[index] = chunkData;
                    index++;
                }

                // Serialize to JSON and store in SaveLoadManager
                string json = JsonUtility.ToJson(saveData, true);
                SaveLoadManager.instance.SetWorldData(sessionName, json);
                File.WriteAllText(saveFilePath, json);
                Debug.Log($"Saved {chunkSaveDatas.Count} chunks with {totalPropCount} props for session {sessionName} to {saveFilePath}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save chunk states for session: {e.Message}");
            }
        }

        public void LoadChunks()
        {
            _loadedChunks.Clear();

            if (Runner == null || Runner.SessionInfo == null || string.IsNullOrEmpty(Runner.SessionInfo.Name))
            {
                Debug.LogWarning("No active session; cannot load chunks.");
                return;
            }

            try
            {
                string sessionName = Runner.SessionInfo.Name;
                string saveFilePath = GetSaveFilePath(sessionName);

                string json;
                // Try to get JSON from SaveLoadManager first
                if (!SaveLoadManager.instance.TryGetWorldData(sessionName, out json))
                {
                    // Fallback to file system if not in SaveLoadManager
                    if (!File.Exists(saveFilePath))
                    {
                        Debug.Log($"No save file found for session {sessionName} at {saveFilePath}. Clearing loaded chunks.");
                        return;
                    }

                    json = File.ReadAllText(saveFilePath);
                    SaveLoadManager.instance.SetWorldData(sessionName, json); // Cache in SaveLoadManager
                }

                FWorldSaveData saveData = JsonUtility.FromJson<FWorldSaveData>(json);
                if (saveData.chunks == null || saveData.chunks.Length == 0)
                {
                    Debug.Log($"No chunk data found for session {sessionName}.");
                    return;
                }

                int totalPropCount = 0; // Manual counter for logging
                foreach (var chunkData in saveData.chunks)
                {
                    FChunkPosition chunkCoord = chunkData.chunkCoord;
                    Debug.Log($"Loading data for Chunk {chunkCoord.X}/{chunkCoord.Y} in session {sessionName}");

                    if (chunkData.props != null && chunkData.props.Length > 0)
                    {
                        // Store the chunk data in loadedChunks
                        _loadedChunks[chunkCoord] = chunkData;
                        totalPropCount += chunkData.props.Length; // Increment prop counter
                    }
                }

                Debug.Log($"Loaded {_loadedChunks.Count} chunks with {totalPropCount} prop states for session {sessionName}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load chunk states for session: {e.Message}");
            }
        }

        private string GetSaveFilePath(string sessionName)
        {
            return SaveLoadManager.instance.GetWorldSaveFilePath(sessionName);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            SaveChunks();
        }
    }
}