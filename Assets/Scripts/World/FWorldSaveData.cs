using UnityEngine;
using System;
using LichLord.NonPlayerCharacters;
using DWD.Utility.Loading;
using LichLord.Items;

namespace LichLord.World
{
    [Serializable]
    public struct FWorldSaveData
    {
        public FChunkSaveData[] chunks;
        public FStrongholdSaveData[] strongholds;
        public FDialogSaveData[] dialogs;
        public FContainerSaveData[] containers;
        public FItemSlotSaveData[] itemSlots;
        public FInvasionSaveData invasion;
    }

    [Serializable]
    public struct FChunkSaveData
    {
        public FChunkPosition chunkCoord;
        public FPropSaveState[] props;
        public FNonPlayerCharacterSaveState[] npcs; // Placeholder for NPC data
    }

    [Serializable]
    public struct FPropSaveState
    {
        public int guid;
        public Vector3 position;
        public Quaternion rotation;
        public int definitionId;
        public int stateData;

        public FPropSaveState(int guid, Vector3 position, Quaternion rotation, int definitionId, int stateData)
        {
            this.guid = guid;
            this.position = position;
            this.rotation = rotation;
            this.definitionId = definitionId;
            this.stateData = stateData;
        }
    }

    [Serializable]
    public struct FWorkerSaveData
    {
        public int index;
        public int strongholdId;
        public bool isAssigned;

        public FWorkerSaveData(int idx, FWorkerData data, bool isAssigned)
        {
            index = idx;
            strongholdId = data.StrongholdID;
            this.isAssigned = isAssigned;
        }

        public FWorkerData ToNetworkWorker()
        {
            FWorkerData netWorker = new FWorkerData();
            netWorker.StrongholdID = (byte)strongholdId;
            netWorker.IsAssigned = isAssigned;

            return netWorker;
        }
    }

    [Serializable]
    public struct FContainerSaveData
    {
        public int containerFullIndex;
        public int startIndex;
        public int endIndex;
        public bool isAssigned;
        public bool isStockpile;

        public FContainerSaveData(int containerFullIndex, 
            int startIndex, 
            int endIndex, 
            bool isAssigned, 
            bool isStockpile)
        {
            this.containerFullIndex = containerFullIndex;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
            this.isAssigned = isAssigned;
            this.isStockpile = isStockpile;
        }
    }

    [Serializable]
    public struct FItemSlotSaveData
    {
        public int fullItemSlotIndex;
        public int definitionId;
        public int data;
        public bool isAssigned;

        public FItemSlotSaveData(int itemFullIndex, FItemSlotData itemSlotData)
        {
            this.fullItemSlotIndex = itemFullIndex;
            this.definitionId = itemSlotData.ItemData.DefinitionID;
            this.data = itemSlotData.ItemData.Data;
            this.isAssigned = itemSlotData.IsAssigned;
        }

        public FItemSlotData ToNetworkData()
        { 
            FItemSlotData itemSlotData = new FItemSlotData();
            FItemData itemData = new FItemData();
            itemData.DefinitionID = this.definitionId;
            itemData.Data = this.data;

            itemSlotData.ItemData = itemData;
            itemSlotData.IsAssigned = this.isAssigned;
            return itemSlotData;
        }
    }

    [Serializable]
    public struct FStrongholdSaveData
    {
        // these two identify the position datat
        public FChunkPosition chunkCoord;
        public int index;

        public int currentHealth;
        public int rank;
        public int strongholdId;
        public int containerIndex;

        public FBuildableSaveState[] buildableStates;
        public FWorkerSaveData[] workerSaveDatas;
    }

    [Serializable]
    public struct FBuildableSaveState
    {
        public int index;
        public Vector3 position;
        public Vector3 eulerAngles;
        public int definitionId;
        public int stateData;

        public FBuildableSaveState(int index, Vector3 position, Vector3 eulerAngles, int definitionId, int stateData)
        {
            this.index = index;
            this.position = position;
            this.eulerAngles = eulerAngles;
            this.definitionId = definitionId;
            this.stateData = stateData;
        }
    }

    [Serializable]
    public struct FNonPlayerCharacterSaveState
    {
        public Vector3 position;
        public Quaternion rotation;
        public int configuration;
        public int condition;
        public int events;
        public FItemSaveData carriedItem;

        // Store harvesting target data here as well
        public FNonPlayerCharacterSaveState(NonPlayerCharacter npc, FNonPlayerCharacterData data)
        {
            position = data.Position;
            rotation = data.Rotation;
            this.configuration = data.Configuration;
            this.condition = data.Condition;
            this.events = data.Events;
            this.carriedItem = new FItemSaveData(data.CarriedItem);
        }
    }

    [Serializable]
    public struct FItemSaveData
    {
        public int definitionId;
        public int data;

        public FItemSaveData(FItemData fromItem)
        {
            this.definitionId = fromItem.DefinitionID;
            this.data = fromItem.Data;
        }

        public FItemData ToNetworkItem()
        { 
            FItemData itemData = new FItemData();
            itemData.DefinitionID = this.definitionId;
            itemData.Data = this.data;
            return itemData;
        }
    }

    [Serializable]
    public struct FWorldMissionSaveState
    {
        public int tutorialProgress;


        // Store harvesting target data here as well
        public FWorldMissionSaveState(int progress)
        {
            this.tutorialProgress = progress;
        }
    }

    [Serializable]
    public struct FDialogSaveData
    {
        public int index;
        public int definitionID;
        public bool isAssigned;

        public FDialogSaveData(int index, int definitionId, bool isAssigned)
        {
            this.index = index;
            this.definitionID = definitionId;
            this.isAssigned = isAssigned;
        }

        public FDialogData ToNetworkDialog()
        {
            FDialogData netDialog = new FDialogData();
            netDialog.DefinitionID = (ushort)definitionID;
            netDialog.IsAssigned = isAssigned;

            return netDialog;
        }
    }

    [Serializable]
    public struct FInvasionSaveData
    {
        public int invasionId;
        public int invasionSpawnWave;
        public Vector3 invasionSpawnPosition;
        public int targetStrongholdId;
        public EInvasionState invasionState;

        public FInvasionSaveData(int invasionId, 
            int invasionSpawnWave, 
            Vector3 invasionSpawnPosition,
            int targetStrongholdId,
            EInvasionState invasionState)
        {
            this.invasionId = invasionId;
            this.invasionSpawnWave = invasionSpawnWave;
            this.invasionSpawnPosition = invasionSpawnPosition;
            this.targetStrongholdId = targetStrongholdId;
            this.invasionState = invasionState;
        }
    }

    
    [Serializable]
    public struct FNPCSaveData
    {
        public FNonPlayerCharacterSaveState[] npcs;
    }
}