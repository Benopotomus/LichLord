using UnityEngine;
using System;
using LichLord.NonPlayerCharacters;

namespace LichLord.World
{
    [Serializable]
    public struct FWorldSaveData
    {
        public FChunkSaveData[] chunks;
        public FStrongholdSaveData[] strongholds;
        public FStockpileSaveData[] stockpiles;
        public FWorkerSaveData[] workers;
        public FDialogSaveData[] dialogs;
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
    public struct FStockpileSaveData
    {
        public int index;
        public FCurrencyStackSaveData pile0;
        public FCurrencyStackSaveData pile1;
        public FCurrencyStackSaveData pile2;
        public FCurrencyStackSaveData pile3;
        public bool isAssigned;

        public FStockpileSaveData(int idx, FStockpileData data, bool isAssigned)
        {
            index = idx;
            pile0 = new FCurrencyStackSaveData(data.GetCurrencyStack(0));
            pile1 = new FCurrencyStackSaveData(data.GetCurrencyStack(1));
            pile2 = new FCurrencyStackSaveData(data.GetCurrencyStack(2));
            pile3 = new FCurrencyStackSaveData(data.GetCurrencyStack(3));
            this.isAssigned = isAssigned;
        }

        public FStockpileData ToNetworkStockpile()
        {
            FStockpileData netStockpile = new FStockpileData();
            netStockpile.AddToStockpile(pile0.currencyType, pile0.value);
            netStockpile.AddToStockpile(pile1.currencyType, pile1.value);
            netStockpile.AddToStockpile(pile2.currencyType, pile2.value);
            netStockpile.AddToStockpile(pile3.currencyType, pile3.value);
            netStockpile.IsAssigned = isAssigned;
            return netStockpile;
        }
    }

    [Serializable]
    public struct FCurrencyStackSaveData
    {
        public ECurrencyType currencyType;
        public byte value;

        public FCurrencyStackSaveData(FCurrencyStack stack)
        {
            currencyType = stack.CurrencyType;
            value = stack.Value;
        }

        public FCurrencyStack ToNetworkStack()
        {
            return new FCurrencyStack
            {
                CurrencyType = currencyType,
                Value = value
            };
        }
    }

    [Serializable]
    public struct FWorkerSaveData
    {
        public int index;
        public int zoneID;
        public int buildableIndex;
        public bool isAssigned;

        public FWorkerSaveData(int idx, FWorkerData data, bool isAssigned)
        {
            index = idx;
            zoneID = data.ZoneID;
            buildableIndex = data.BuildableIndex;
            this.isAssigned = isAssigned;
        }

        public FWorkerData ToNetworkWorker()
        {
            FWorkerData netWorker = new FWorkerData();
            netWorker.ZoneID = (byte)zoneID;
            netWorker.BuildableIndex = (ushort)buildableIndex;
            netWorker.IsAssigned = isAssigned;

            return netWorker;
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
        public int buildableZoneID;

        public FBuildableSaveState[] buildableStates;
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

        // Store harvesting target data here as well
        public FNonPlayerCharacterSaveState(NonPlayerCharacter npc, FNonPlayerCharacterData data)
        {
            position = data.Position;
            rotation = data.Rotation;
            this.configuration = data.Configuration;
            this.condition = data.Condition;
            this.events = data.Events;
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
        public FTargetStrongholdSaveData targetStronghold;
        public EInvasionState invasionState;

        public FInvasionSaveData(int invasionId, 
            int invasionSpawnWave, 
            Vector3 invasionSpawnPosition,
            FStrongholdData targetStronghold,
            EInvasionState invasionState)
        {
            this.invasionId = invasionId;
            this.invasionSpawnWave = invasionSpawnWave;
            this.invasionSpawnPosition = invasionSpawnPosition;
            this.targetStronghold.chunkCoord = targetStronghold.ChunkID;
            this.targetStronghold.index = targetStronghold.ChunkIndex;
            this.invasionState = invasionState;
        }

        [Serializable]
        public struct FTargetStrongholdSaveData
        {
            public FChunkPosition chunkCoord;
            public int index;
        }
        }

    [Serializable]
    public struct FNPCSaveData
    {
        public FNonPlayerCharacterSaveState[] npcs;
    }
}