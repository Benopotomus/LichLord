using LichLord.World;
using System;
using UnityEngine;

namespace LichLord.Props
{
    public class PropRuntimeState
    {
        public Chunk chunk; // Owning chunk
        public int index; // Unique identifier
        public int definitionId; // PropDefinition.TableID

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
            Chunk chunk,
            Vector3 position, 
            Quaternion rotation, 
            int definitionId)
        {
            this.index = guid;
            this.chunk = chunk;
            this.definitionId = definitionId;
            this.position = position;
            this.rotation = rotation;

            PropDefinition definition = Global.Tables.PropTable.TryGetDefinition(definitionId);
            PropDataDefinition dataDefinition = definition.PropDataDefinition;

            _data.DefinitionID = definitionId;
            dataDefinition.InitializeData(ref _data, definition);
        }

        public PropRuntimeState(int guid,
            Chunk chunk,
            Vector3 position,
            Quaternion rotation,
            int definitionId,
            FPropData propData)
        {
            this.index = guid;
            this.chunk = chunk;
            this.definitionId = definitionId;
            this.position = position;
            this.rotation = rotation;

            _data.Copy(ref propData);
            _data.DefinitionID = definitionId;
        }

        public PropRuntimeState(PropRuntimeState other)
        {
            this.index = other.index;
            this.definitionId = other.definitionId;
            this.position = other.position;
            this.rotation = other.rotation;

            FPropData otherData = other._data;
            _data.Copy(ref otherData);
            _data.DefinitionID = definitionId;
        }

        public EPropState GetState()
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            return dataDefinition.GetState(ref _data);
        }

        public int GetHealth()
        {
            if (Definition.PropDataDefinition is DestructiblePropDataDefinition destructibleDataDefinition)
                return destructibleDataDefinition.GetHealth(ref _data);

            return 0;
        }

        public int GetMaxHealth()
        {
            if (Definition.PropDataDefinition is DestructiblePropDataDefinition destructibleDataDefinition)
                return destructibleDataDefinition.MaxHealth;

            return 0;
        }

        public void ApplyDamage(int damage, int tick)
        {
            if (Definition.PropDataDefinition is DestructiblePropDataDefinition destructibleDataDefinition)
                destructibleDataDefinition.ApplyDamage(ref _data, damage);

            _hitReactEndTick = _hitReactTicks + tick;
        }

        public void Harvest(int harvestValue, int tick)
        {
            if (Definition.PropDataDefinition is HarvestNodeDataDefinition harvestDataDefinition)
                harvestDataDefinition.ApplyHarvest(ref _data, harvestValue);

            _hitReactEndTick = _hitReactTicks + tick;
        }

        public void SetInteract(bool interact, int tick)
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;

            if (dataDefinition is NexusDataDefinition nexusDataDefinition)
            {
                nexusDataDefinition.SetIsInteracting(interact, ref _data);
            }
        }

        public void SetActivated(bool activated, int tick)
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;

            if (dataDefinition is NexusDataDefinition nexusDataDefinition)
            {
                Debug.Log("Set Activated: " + activated);
                nexusDataDefinition.SetIsActivated(activated, ref _data);
            }
        }

        public bool GetIsInteracting()
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            if (dataDefinition is NexusDataDefinition nexusDataDefinition)
            {
                // Create a propdata and set its state data to get current health
                return nexusDataDefinition.GetIsInteracting(ref _data);
            }

            return false;
        }

        public bool GetIsActivated()
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            if (dataDefinition is NexusDataDefinition nexusDataDefinition)
            {
                // Create a propdata and set its state data to get current health
                return nexusDataDefinition.GetIsActivated(ref _data);
            }

            return false;
        }

        public void CopyData(ref FPropData propData)
        { 
            _data.Copy(ref propData);
        }

        // Runtime Values

        EPropState _currentState;
        int _hitReactTicks = 8;
        int _hitReactEndTick;

        // Updates on the server if the RuntimePropState is loaded
        // Does not require the monobehaviour to exist
        public bool AuthorityUpdate(int tick)
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            _currentState = dataDefinition.GetState(ref _data);

            switch (_currentState)
            {
                case EPropState.HitReact:
                    if (tick > _hitReactEndTick)
                    {
                        _currentState = EPropState.Idle;
                        dataDefinition.SetState( _currentState, ref _data);
                        return true;
                    }
                    break;
                case EPropState.Destroyed:
                    break;
            }

            return false;
        }
    }
}