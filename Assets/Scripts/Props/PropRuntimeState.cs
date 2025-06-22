using System;
using UnityEngine;

namespace LichLord.Props
{
    [Serializable]
    public class PropRuntimeState
    {
        public int guid; // Unique identifier
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

            _data.GUID = guid;
            _data.DefinitionID = definitionId;
            dataDefinition.InitializeData(ref _data, definition);
        }

        public PropRuntimeState(int guid,
            Vector3 position,
            Quaternion rotation,
            int definitionId,
            FPropData propData)
        {
            this.guid = guid;
            this.definitionId = definitionId;
            this.position = position;
            this.rotation = rotation;

            _data.Copy(ref propData);
            _data.GUID = guid;
            _data.DefinitionID = definitionId;
        }

        public PropRuntimeState(PropRuntimeState other)
        {
            this.guid = other.guid;
            this.definitionId = other.definitionId;
            this.position = other.position;
            this.rotation = other.rotation;

            FPropData otherData = other.Data;
            _data.Copy(ref otherData);
            _data.GUID = guid;
            _data.DefinitionID = definitionId;
        }

        public EPropState GetState()
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            return dataDefinition.GetState(ref _data);
        }

        public int GetHealth()
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            return dataDefinition.GetHealth(ref _data);
        }

        public void ApplyDamage(int damage)
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;

            // Create a propdata and set its state data to get current health
            dataDefinition.ApplyDamage(ref _data, damage);

            _hitReactTimer = _hitReactTimeMax;

            if (GetState() == EPropState.Destroyed)
                _deadTimer = _deadTimeMax;
        }

        public void CopyData(ref FPropData propData)
        { 
            _data.Copy(ref propData);
        }

        // Runtime Updates

        // Runtime Values

        EPropState _currentState;
        float _hitReactTimeMax = 0.25f;
        float _hitReactTimer = 0.25f;

        float _deadTimeMax = 3.0f;
        float _deadTimer = 3.0f;

        // Updates on the server if the RuntimePropState is loaded
        // Does not require the monobehaviour to exist
        public bool AuthorityUpdate(float networkDeltaTime)
        {
            PropDataDefinition dataDefinition = Definition.PropDataDefinition;
            _currentState = dataDefinition.GetState(ref _data);

            switch (_currentState)
            {
                case EPropState.HitReact:

                    _hitReactTimer -= networkDeltaTime;
                    if (_hitReactTimer < 0f)
                    {
                        _currentState = EPropState.Idle;
                        dataDefinition.SetState( _currentState, ref _data);
                        return true;
                    }
                    break;
                case EPropState.Destroyed:
                    /*
                    _deadTimer -= networkDeltaTime;
                    if (_deadTimer < 0f)
                    {
                        _currentState = EPropState.Inactive;
                        dataDefinition.SetState(_currentState, ref _data);
                        return true;
                    }
                     */
                    break;
            }

            return false;
        }
    }
}