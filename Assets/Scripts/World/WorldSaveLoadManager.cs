using UnityEngine;
using System.IO;
using Fusion;
using System.Collections.Generic;
using System;
using System.Linq;

namespace LichLord.World
{
    public class WorldSaveLoadManager : ContextBehaviour
    {
        [SerializeField] private string saveFileName = "WorldSaveData.json";
        private string saveFilePath;

        private Dictionary<FChunkPosition, FChunkSaveData> _loadedChunks = new Dictionary<FChunkPosition, FChunkSaveData>();
        public Dictionary<FChunkPosition, FChunkSaveData> LoadedChunks => _loadedChunks;

        private void Awake()
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        }

        public void DeleteSavedChunkData()
        {

        }

        private void SaveChunks()
        {
            try
            {
                // Get all the world chunks
                var worldChunks = Context.ChunkManager.WorldChunks;

                // Hold onto a list of chunksavedatas. Add to this list for saving
                Dictionary<FChunkPosition, FChunkSaveData> chunkSaveDatas = new Dictionary<FChunkPosition, FChunkSaveData>();

                foreach (var chunkPair in worldChunks)
                {
                    FChunkPosition chunkCoord = chunkPair.Key;
                    Chunk chunk = chunkPair.Value;
                    if (chunk.DeltaPropStates == null || chunk.DeltaPropStates.Count == 0)
                    {
                        continue;
                    }

                    List <FPropSaveState> propList = new List<FPropSaveState>();
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
                    }
                }

                // Convert to WorldSaveData for serialization
                FWorldSaveData saveData = new FWorldSaveData
                {
                    chunks = chunkSaveDatas.Values.ToArray()
                };

                // Serialize to JSON
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(saveFilePath, json);
                Debug.Log($"Saved {chunkSaveDatas.Count} chunks with {chunkSaveDatas.Values.Sum(chunk => chunk.props?.Length ?? 0)} props to {saveFilePath}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save chunk states to {saveFilePath}: {e.Message}");
            }
        }

        public void LoadChunks()
        {
            if (!File.Exists(saveFilePath))
            {
                Debug.Log($"No save file found at {saveFilePath}. Returning empty chunk dictionary.");
                return;
            }

            try
            {
                string json = File.ReadAllText(saveFilePath);
                FWorldSaveData saveData = JsonUtility.FromJson<FWorldSaveData>(json);
                if (saveData.chunks == null || saveData.chunks.Length == 0)
                {
                    Debug.Log($"No chunk data found in {saveFilePath}.");
                    return;
                }

                int totalPropCount = 0; // Manual counter for logging
                foreach (var chunkData in saveData.chunks)
                {
                    FChunkPosition chunkCoord = chunkData.chunkCoord;
                    Debug.Log($"Loading data for Chunk {chunkCoord.X}/{chunkCoord.Y}");

                    if (chunkData.props != null && chunkData.props.Length > 0)
                    {
                        // Store the chunk data in loadedChunks
                        _loadedChunks[chunkCoord] = chunkData;
                        totalPropCount += chunkData.props.Length; // Increment prop counter
                    }
                }

                Debug.Log($"Loaded {_loadedChunks.Count} chunks with {totalPropCount} prop states from {saveFilePath}.");
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load chunk states from {saveFilePath}: {e.Message}");
                return;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            SaveChunks();
        }
    }
}