using System;
using UnityEngine;

namespace LichLord.Props
{
    [Serializable]
    public class PropMarkupData
    {
        public int guid;
        public Vector3 position;
        public Quaternion rotation; // Forward direction (unit vector)
        public PropDefinition propDefinition;

        // Only for terrains
        public string terrainId;            // Unique ID of the terrain
        public Vector3 terrainTreePosition; // Tree's world-space position
    }
}