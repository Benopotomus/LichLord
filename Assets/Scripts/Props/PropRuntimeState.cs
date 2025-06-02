using System;
using UnityEngine;

namespace LichLord.Props
{
    [Serializable]
    public class PropRuntimeState
    {
        public int guid; // Unique identifier
        public Vector3 position; // World position
        public Quaternion rotation; // World rotation
        public int definitionId; // PropDefinition.TableID
        public int stateData; // Custom runtime data (like FPropData.Data)
        public PropReplicationData replicationData;

        public PropRuntimeState(int guid, 
            Vector3 position, 
            Quaternion rotation, 
            int propDefinitionId, 
            int data)
        {
            this.guid = guid;
            this.position = position;
            this.rotation = rotation;
            this.definitionId = propDefinitionId;
            this.stateData = data;
        }
    }
}