using System;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public class NonPlayerCharacterRuntimeState
    {
        public int guid; // Unique identifier, for saves
        public int definitionId; // PropDefinition.TableID
        public Vector3 position; // World position
        public Quaternion rotation; // World rotation
        public int stateData; // Custom runtime data (like FPropData.Data)
        public int health;

        public NonPlayerCharacterRuntimeState(int guid,
            int definitionId,
            Vector3 position, 
            Quaternion rotation, 
            int stateData,
            int health)
        {
            this.guid = guid;
            this.position = position;
            this.rotation = rotation;
            this.definitionId = definitionId;
            this.stateData = stateData;
        }
    }
}