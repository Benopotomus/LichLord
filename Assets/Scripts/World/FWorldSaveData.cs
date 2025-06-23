using UnityEngine;
using System;

namespace LichLord.World
{
    [Serializable]
    public struct FWorldSaveData
    {
        public FChunkSaveData[] chunks;
        public FPlayerSaveState[] players; // Players
    }

    [Serializable]
    public struct FChunkSaveData
    {
        public FChunkPosition chunkCoord;
        public FPropSaveState[] props;
        public NPCSaveState[] npcs; // Placeholder for NPC data
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
    public struct NPCSaveState
    {
        public int guid;
        public Vector3 position;
        public Quaternion rotation;
        public int definitionId;
        public int health; // Example NPC-specific field
        public string aiState; // Example NPC-specific field

        public NPCSaveState(int guid, Vector3 position, Quaternion rotation, int definitionId, int health, string aiState)
        {
            this.guid = guid;
            this.position = position;
            this.rotation = rotation;
            this.definitionId = definitionId;
            this.health = health;
            this.aiState = aiState;
        }
    }

    [Serializable]
    public struct FPlayerSaveState
    {
        public string playerName;
        public Vector3 position;
        public Quaternion rotation;

        public FPlayerSaveState(string playerName, Vector3 position, Quaternion rotation)
        {
            this.playerName = playerName;
            this.position = position;
            this.rotation = rotation;

        }
    }
}