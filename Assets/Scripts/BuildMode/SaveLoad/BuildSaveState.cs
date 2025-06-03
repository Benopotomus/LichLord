using System;
using UnityEngine;

namespace LichLord.Buildables
{
    [Serializable]
    public class BuildSaveState
    {
        public int guid; // Unique identifier
        public Vector3 position; // World position
        public Quaternion rotation; // World rotation
        public int definitionId; // PropDefinition.TableID
        public int stateData; // Custom runtime data (like FPropData.Data)

        public BuildSaveState(int guid, Vector3 position, Quaternion rotation, int propDefinitionId, int stateData)
        {
            this.guid = guid;
            this.position = position;
            this.rotation = rotation;
            this.definitionId = propDefinitionId;
            this.stateData = stateData;
        }
    }
}