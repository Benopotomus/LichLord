using System;
using UnityEngine;

namespace LichLord.Buildables
{
    [Serializable]
    public class BuildableRuntimeState
    {
        public int definitionId; // Unique identifier
        public Vector3 position; // World position
        public Quaternion rotation; // World rotation
        public int data;

        public BuildableRuntimeState(int definitionId,
            Vector3 position,
            Quaternion rotation,
            int data)
        {
            this.definitionId = definitionId;
            this.position = position;
            this.rotation = rotation;
            this.data = data;
        }
    }
}