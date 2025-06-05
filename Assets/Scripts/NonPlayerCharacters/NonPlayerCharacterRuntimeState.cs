using System;
using System.Data;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public class NonPlayerCharacterRuntimeState
    {
        public int index; // Unique identifier, for saves
        public int definitionId; // NonPlayerCharacterDefinition.TableID
        public Vector3 position; // World position
        public Quaternion rotation; // World rotation
        public Vector3 velocity;
        public int stateData; // Custom runtime data
        public int health;

        // For the master client, easy access to the replicator helps
        // writing data on each update.
        public NonPlayerCharacterReplicator replicator;

        public NonPlayerCharacterRuntimeState() { }

        public NonPlayerCharacterRuntimeState(int index,
            int definitionId,
            Vector3 position, 
            Quaternion rotation, 
            Vector3 velocity,
            int stateData,
            int health)
        {
            this.index = index;
            this.position = position;
            this.rotation = rotation;
            this.velocity = velocity;
            this.definitionId = definitionId;
            this.stateData = stateData;
        }

        public void SetState(ref FNonPlayerCharacterData data)
        {
            definitionId = data.DefinitionID;
            position = data.Transform.Position;
            rotation = data.Transform.Rotation;
            //velocity = data.Velocity;
            stateData = data.StateData;
            health = data.Health;
        }

    }
}