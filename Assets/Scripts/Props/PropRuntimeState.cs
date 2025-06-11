using System;
using UnityEngine;

namespace LichLord.Props
{
    [Serializable]
    public class PropRuntimeState
    {
        public int guid; // Unique identifier
        public int definitionId; // PropDefinition.TableID
        public int stateData; // Custom runtime data (like FPropData.Data)

        // Not replicated
        public Vector3 position; // World position
        public Quaternion rotation; // World rotation

        public PropRuntimeState(int guid, 
            Vector3 position, 
            Quaternion rotation, 
            int definitionId, 
            int stateData)
        {
            this.guid = guid;
            this.definitionId = definitionId;
            this.stateData = stateData;
            this.position = position;
            this.rotation = rotation;
        }

        public PropRuntimeState(PropRuntimeState other)
        {
            this.guid = other.guid;
            this.definitionId = other.definitionId;
            this.stateData = other.stateData;
            this.position = other.position;
            this.rotation = other.rotation;
        }

        public bool UpdateState(float deltaTime)
        {
            return false;
        }

        public void ApplyDamage(int damage)
        {
            stateData = 1;
        }
    }
}