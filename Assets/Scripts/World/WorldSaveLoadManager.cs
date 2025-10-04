using UnityEngine;
using System.IO;
using Fusion;
using System.Collections.Generic;
using System;
using LichLord.Buildables;
using LichLord.NonPlayerCharacters;
using LichLord.Dialog;
using LichLord.Items;

namespace LichLord.World
{
    public class WorldSaveLoadManager : ContextBehaviour
    {
        private Dictionary<FChunkPosition, FChunkSaveData> _loadedChunks = new Dictionary<FChunkPosition, FChunkSaveData>();
        public Dictionary<FChunkPosition, FChunkSaveData> LoadedChunks => _loadedChunks;

        private List<FStrongholdSaveData> _loadedStrongholds = new List<FStrongholdSaveData>();
        public List<FStrongholdSaveData> LoadedStrongholds => _loadedStrongholds;

        private List<FNonPlayerCharacterSaveState> _loadedNPCs = new List<FNonPlayerCharacterSaveState>();
        public List<FNonPlayerCharacterSaveState> LoadedNPCs => _loadedNPCs;

        private FInvasionSaveData _loadedInvasion = new FInvasionSaveData();
        public FInvasionSaveData LoadedInvasion => _loadedInvasion;

        private List<FContainerSaveData> _loadedContainers = new List<FContainerSaveData>();
        public List<FContainerSaveData> LoadedContainers => _loadedContainers;

        private List<FItemSlotSaveData> _loadedItemSlots = new List<FItemSlotSaveData>();
        public List<FItemSlotSaveData> LoadedItemSlots => _loadedItemSlots;

        private FusionCallbacksHandler _shutdownHandler;

        public override void Spawned()
        {
            base.Spawned();
            _shutdownHandler = new FusionCallbacksHandler();
            _shutdownHandler.Shutdown += OnFusionShutdown;

            Runner.AddCallbacks(_shutdownHandler);
        }

        private void SaveWorld()
        {
            if (Runner == null)
            {
                Debug.LogWarning("No active session; cannot save world.");
                return;
            }

            try
            {
                string sessionName = Global.Networking.SessionName;
                string saveFilePath = GetWorldSaveFilePath(sessionName);

                // --- Save chunks ---
                var deltaChunks = Context.ChunkManager.DeltaChunks;
                List<FChunkSaveData> chunkSaveDatas = new List<FChunkSaveData>();
                int totalPropCount = 0;

                foreach (var chunk in deltaChunks)
                {
                    if (chunk.DeltaPropStates == null || chunk.DeltaPropStates.Count == 0)
                        continue;

                    List<FPropSaveState> propList = new List<FPropSaveState>();
                    foreach (var deltaState in chunk.DeltaPropStates.Values)
                    {
                        if (deltaState == null)
                        {
                            Debug.LogWarning($"Null PropRuntimeState for chunk {chunk.ChunkID.X}/{chunk.ChunkID.Y}.");
                            continue;
                        }
                        propList.Add(new FPropSaveState(
                            deltaState.index,
                            deltaState.position,
                            deltaState.rotation,
                            deltaState.definitionId,
                            deltaState.Data.StateData
                        ));
                    }

                    if (propList.Count > 0)
                    {
                        chunkSaveDatas.Add(new FChunkSaveData
                        {
                            chunkCoord = chunk.ChunkID,
                            props = propList.ToArray()
                        });
                        totalPropCount += propList.Count;
                    }
                }

                // --- Save strongholds ---
                List<FStrongholdSaveData> strongholdSaveDatas = new List<FStrongholdSaveData>();
                foreach (var stronghold in Context.StrongholdManager.ActiveStrongholds)
                {
                    var buildableList = new List<FBuildableSaveState>();
                    var buildableZone = stronghold.BuildableZone;
                    var buildData = buildableZone.Data;

                    for (int i = 0; i < buildData.Length; i++)
                    {
                        if (buildableZone.RuntimeStates.TryGetValue(i, out BuildableRuntimeState runtimeState))
                        {
                            runtimeState.SetInteracting(false, 0);

                            buildableList.Add(new FBuildableSaveState(
                                i,
                                runtimeState.Data.Position,
                                runtimeState.Data.Rotation.eulerAngles,
                                runtimeState.Data.DefinitionID,
                                runtimeState.Data.StateData
                            ));
                        }
                    }

                    List<FWorkerSaveData> workerSaveData = new List<FWorkerSaveData>();

                    for (int i = 0; i < BuildableConstants.MAX_WORKERS_PER_STRONGHOLD; i++)
                    {
                        FWorkerData workerData = stronghold.WorkerComponent.GetWorkerData(i);
                        workerSaveData.Add(new FWorkerSaveData(i, workerData, workerData.IsAssigned));
                    }

                    strongholdSaveDatas.Add(new FStrongholdSaveData
                    {
                        chunkCoord = stronghold.Data.ChunkID,
                        index = stronghold.Data.Index,
                        currentHealth = stronghold.CurrentHealth,
                        rank = stronghold.Rank,
                        strongholdId = stronghold.StrongholdID,
                        containerIndex = stronghold.ContainerIndex,
                        buildableStates = buildableList.ToArray(),
                        workerSaveDatas = workerSaveData.ToArray()
                    });
                }

                // --- Save DialogSaveDatas --- //
                List<FDialogSaveData> dialogSaveDatas = new List<FDialogSaveData>();
                if (Context.DialogManager != null)
                {
                    for (int i = 0; i < DialogConstants.MAX_DIALOGS; i++)
                    {
                        FDialogData dialogData = Context.DialogManager.GetDialog(i);
                        dialogSaveDatas.Add(new FDialogSaveData(i, dialogData.DefinitionID, dialogData.IsAssigned));
                    }
                }

                // --- Save Invasion Data --- //

                FInvasionSaveData invasionSaveData = new FInvasionSaveData();

                if (Context.InvasionManager != null)
                {
                    InvasionManager invasionManager = Context.InvasionManager;
                    invasionSaveData.invasionId = invasionManager.InvasionID;
                    invasionSaveData.invasionSpawnWave = invasionManager.InvasionSpawnWave;
                    invasionSaveData.invasionSpawnPosition = invasionManager.InvasionStagingPosition.Position;
                    invasionSaveData.targetStrongholdId = invasionManager.TargetStrongholdID;
                    invasionSaveData.invasionState = invasionManager.InvasionState;
                }

                // --- Save Invasion Data --- //

                List<FContainerSaveData> containerSaveData = new List<FContainerSaveData>();
                List<FItemSlotSaveData> itemSlotSaveData = new List<FItemSlotSaveData>();

                if (Context.ContainerManager != null)
                {
                    ContainerManager containerManager = Context.ContainerManager;

                    foreach (var containerReplicator in containerManager.ContainerReplicators)
                    { 
                        var containerDatas = containerReplicator.ContainerDatas;

                        for (int i = 0; i < containerDatas.Length; i++)
                        {
                            FContainerSlotData containerSlotData = containerDatas[i];
                            int fullContainerIndex = i + (containerReplicator.Index * ContainerConstants.CONTAINERS_PER_REPLICATOR);
                            containerSaveData.Add(new FContainerSaveData
                            {
                                containerFullIndex = fullContainerIndex,
                                startIndex = containerSlotData.StartIndex,
                                endIndex = containerSlotData.EndIndex,
                                isAssigned = containerSlotData.IsAssigned,
                                isStockpile = containerSlotData.IsStockpile
                            });
                        }
                    }

                    foreach (var itemSlotReplicator in containerManager.ItemSlotReplicators)
                    {
                        var itemSlotDatas = itemSlotReplicator.ItemSlotDatas;

                        for (int i = 0; i < itemSlotDatas.Length; i++)
                        {
                            FItemSlotData itemSlotData = itemSlotDatas[i];
                            int fullItemSlotIndex = i + (itemSlotReplicator.Index * ContainerConstants.ITEMS_PER_REPLICATOR);

                            itemSlotSaveData.Add(new FItemSlotSaveData
                            {
                                fullItemSlotIndex = fullItemSlotIndex,
                                definitionId = itemSlotData.ItemData.DefinitionID,
                                data = itemSlotData.ItemData.Data,
                                isAssigned = itemSlotData.IsAssigned,
                            });
                        }
                    }
                }

                // --- Final save ---
                FWorldSaveData saveData = new FWorldSaveData
                {
                    chunks = chunkSaveDatas.ToArray(),
                    strongholds = strongholdSaveDatas.ToArray(),
                    dialogs = dialogSaveDatas.ToArray(),
                    invasion = invasionSaveData,
                    containers = containerSaveData.ToArray(),
                    itemSlots = itemSlotSaveData.ToArray(),
                };

                string json = JsonUtility.ToJson(saveData, true);
                SaveLoadManager.instance.SetWorldData(sessionName, json);
                File.WriteAllText(saveFilePath, json);

                Debug.Log($"Saved world: {chunkSaveDatas.Count} chunks, {totalPropCount} props, {strongholdSaveDatas.Count} strongholds");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save world: {e}");
            }
        }

        public void LoadWorld()
        {
            _loadedChunks.Clear();
            _loadedStrongholds.Clear();

            if (Runner == null)
            {
                Debug.LogWarning("No active session; cannot load chunks/strongholds.");
                return;
            }

            try
            {
                string sessionName = Global.Networking.SessionName;
                string saveFilePath = GetWorldSaveFilePath(sessionName);

                string json;
                // Try to get JSON from SaveLoadManager first
                if (!SaveLoadManager.instance.TryGetWorldData(sessionName, out json))
                {
                    // Fallback to file system if not in SaveLoadManager
                    if (!File.Exists(saveFilePath))
                    {
                        Debug.Log($"No save file found for session {sessionName} at {saveFilePath}. Clearing loaded chunks/strongholds.");
                        return;
                    }

                    json = File.ReadAllText(saveFilePath);
                    SaveLoadManager.instance.SetWorldData(sessionName, json); // Cache in SaveLoadManager
                }

                FWorldSaveData saveData = JsonUtility.FromJson<FWorldSaveData>(json);

                // --- Load Chunks ---
                int totalPropCount = 0;
                if (saveData.chunks != null && saveData.chunks.Length > 0)
                {
                    foreach (var chunkData in saveData.chunks)
                    {
                        FChunkPosition chunkCoord = chunkData.chunkCoord;
                        Debug.Log($"Loading data for Chunk {chunkCoord.X}/{chunkCoord.Y} in session {sessionName}");

                        if (chunkData.props != null && chunkData.props.Length > 0)
                        {
                            _loadedChunks[chunkCoord] = chunkData;
                            totalPropCount += chunkData.props.Length;
                        }
                    }
                }

                // --- Load Strongholds ---
                if (saveData.strongholds != null && saveData.strongholds.Length > 0)
                {
                    foreach (var strongholdData in saveData.strongholds)
                    {
                        _loadedStrongholds.Add(strongholdData);
                        Debug.Log($"Loaded stronghold {strongholdData.index} in chunk {strongholdData.chunkCoord.X}/{strongholdData.chunkCoord.Y}");

                        // Load the buildables and look for stockpiles to adjust the index
                        foreach (var buildableSave in strongholdData.buildableStates)
                        {
                            if (buildableSave.definitionId == 0)
                                continue;

                            var definition = Global.Tables.BuildableTable.TryGetDefinition(buildableSave.definitionId);
                            FBuildableData data = new FBuildableData();
                            data.LoadFromSave(buildableSave);
                        }
                    }
                }

                // --- Load dialogs ---
                if (saveData.dialogs != null)
                {
                    foreach (var dialogSave in saveData.dialogs)
                    {
                        Context.DialogManager.LoadDialogData(dialogSave);
                    }
                    Debug.Log($"Loaded {saveData.dialogs.Length} dialogs.");
                }

                // --- Load Containers ---
                if (saveData.containers != null)
                {
                    foreach (var containerSave in saveData.containers)
                    {
                        _loadedContainers.Add(containerSave);
                    }
                    Debug.Log($"Loaded {saveData.containers.Length} containers.");
                }

                // --- Load ItemSlots ---
                if (saveData.itemSlots != null)
                {
                    foreach (var itemSlotSave in saveData.itemSlots)
                    {
                        _loadedItemSlots.Add(itemSlotSave);
                    }
                    Debug.Log($"Loaded {saveData.itemSlots.Length} item slots.");
                }

                // --- Load invasion ---

                _loadedInvasion = saveData.invasion;

                //Context.ContainerManager.UpdateAllCurrencies();

                Debug.Log($"Loaded {_loadedChunks.Count} chunks with {totalPropCount} props and {_loadedStrongholds.Count} strongholds for session {sessionName}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load world states for session: {e.Message}");
            }
        }

        private void SaveNPCs()
        {
            if (Runner == null)
            {
                Debug.LogWarning("No active session; cannot save NPCs.");
                return;
            }

            try
            {
                string sessionName = Global.Networking.SessionName;
                string saveFilePath = GetNPCSaveFilePath(sessionName);

                List<FNonPlayerCharacterSaveState> npcSaves = Context.NonPlayerCharacterManager.GetAllSaveStates();

                FNPCSaveData saveData = new FNPCSaveData
                {
                    npcs = npcSaves.ToArray()
                };

                string json = JsonUtility.ToJson(saveData, true);

                SaveLoadManager.instance.SetNPCData(sessionName, json);
                File.WriteAllText(saveFilePath, json);

                Debug.Log($"Saved {npcSaves.Count} NPCs for session {sessionName} to {saveFilePath}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save NPC states for session: {e.Message}");
            }
        }

        public void LoadNPCs()
        {
            _loadedNPCs.Clear();

            if (Runner == null)
            {
                Debug.LogWarning("No active session; cannot load NPCs.");
                return;
            }

            try
            {
                string sessionName = Global.Networking.SessionName;

                string saveFilePath = GetNPCSaveFilePath(sessionName);

                string json;
                // Try to get JSON from SaveLoadManager first
                if (!SaveLoadManager.instance.TryGetNPCData(sessionName, out json))
                {
                    // Fallback to file system if not in SaveLoadManager
                    if (!File.Exists(saveFilePath))
                    {
                        Debug.Log($"No NPC save file found for session {sessionName} at {saveFilePath}. Clearing loaded NPCs.");
                        return;
                    }

                    json = File.ReadAllText(saveFilePath);
                    SaveLoadManager.instance.SetNPCData(sessionName, json); // Cache in SaveLoadManager
                }

                FNPCSaveData saveData = JsonUtility.FromJson<FNPCSaveData>(json);
                if (saveData.npcs == null || saveData.npcs.Length == 0)
                {
                    Debug.Log($"No NPC data found for session {sessionName}.");
                    return;
                }

                foreach (var npcSave in saveData.npcs)
                {
                    _loadedNPCs.Add(npcSave);
                }

                Debug.Log($"Loaded {_loadedNPCs.Count} NPCs for session {sessionName}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load NPC states for session: {e.Message}");
            }
        }

        private string GetWorldSaveFilePath(string sessionName)
        {
            return SaveLoadManager.instance.GetWorldSaveFilePath(sessionName);
        }

        private string GetNPCSaveFilePath(string sessionName)
        {
            return SaveLoadManager.instance.GetNPCSaveFilePath(sessionName);
        }

        private void OnFusionShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            Debug.Log("Fusion shutting down: saving world and NPC state...");

            SaveWorld();
            SaveNPCs();
        }

    }
}