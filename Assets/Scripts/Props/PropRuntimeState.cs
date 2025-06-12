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

        private PropDefinition _definition;
        public PropDefinition Definition
        {
            get
            {
                if (_definition == null)
                    _definition = Global.Tables.PropTable.TryGetDefinition(definitionId);

                return _definition;
            }
        }

        private FPropData _data = new FPropData();
        public FPropData Data => _data;

        public PropRuntimeState(int guid, 
            Vector3 position, 
            Quaternion rotation, 
            int definitionId)
        {
            this.guid = guid;
            this.definitionId = definitionId;
            this.position = position;
            this.rotation = rotation;

            PropDefinition definition = Global.Tables.PropTable.TryGetDefinition(definitionId);
            PropDataDefinition dataDefinition = definition.PropDataDefinition;

            dataDefinition.InitializeData(ref _data, definition);
            this.stateData = _data.StateData; 
        }

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

            _data.StateData = stateData;
        }

        public PropRuntimeState(PropRuntimeState other)
        {
            this.guid = other.guid;
            this.definitionId = other.definitionId;
            this.stateData = other.stateData;
            this.position = other.position;
            this.rotation = other.rotation;

            _data.StateData = other.stateData;
        }

        public bool UpdateState(float deltaTime)
        {
            return false;
        }

        public EPropState GetState()
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            return dataDefinition.GetState(ref _data);
        }

        public void ApplyDamage(int damage)
        {
            PropDefinition definition = Global.Tables.PropTable.TryGetDefinition(definitionId);
            PropDataDefinition dataDefinition = definition.PropDataDefinition;

            // Create a propdata and set its state data to get current health
            dataDefinition.ApplyDamage(ref _data, damage);

            // Set the stateData back locally
            stateData = _data.StateData;
        }
    }
}