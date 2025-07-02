using UnityEngine;
using System;
using LichLord.NonPlayerCharacters;

namespace LichLord.World
{
    [Serializable]
    public struct FWorldSaveData
    {
        public FChunkSaveData[] chunks;
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
    public struct FNPCSaveData
    {
        public FNonPlayerCharacterSaveState[] npcs;
    }
}