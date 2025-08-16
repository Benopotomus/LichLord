using LichLord.Props;
using System;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableRuntimeState
    {
        public int index; // index in buildable zone
        public int definitionId;
        public Vector3 position; // World position
        public Quaternion rotation; // World rotation
        public int stateData;

        private BuildableDefinition _definition;
        public BuildableDefinition Definition
        {
            get
            {
                if (_definition == null)
                    _definition = Global.Tables.BuildableTable.TryGetDefinition(definitionId);

                return _definition;
            }
        }

        private BuildableDataDefinition _dataDefinition;
        public BuildableDataDefinition DataDefinition
        {
            get
            {
                if (_dataDefinition == null)
                    _dataDefinition = Definition.BuildableDataDefinition;

                return _dataDefinition;
            }
        }

        private FBuildableData _data = new FBuildableData();
        public FBuildableData Data => _data;

        public BuildableRuntimeState(int index, ref FBuildableData buildableData)
        {
            this.index = index;
            _data.Copy(ref buildableData);
            definitionId = _data.DefinitionID;
            position = _data.Position;
            rotation = _data.Rotation;
            stateData = _data.StateData;
        }

        public void CopyData(ref FBuildableData buildableData)
        {
            _data.Copy(ref buildableData);
            definitionId = _data.DefinitionID;
            position = _data.Position;
            rotation = _data.Rotation;
            stateData = _data.StateData;
        }

        public EBuildableState GetState()
        {
            BuildableDataDefinition dataDefinition = Definition.BuildableDataDefinition;
            return dataDefinition.GetState(ref _data);
        }

        public int GetHealth()
        {
            if (Definition.BuildableDataDefinition is DestructibleBuildableDataDefinition destructibleDataDefinition)
                return destructibleDataDefinition.GetHealth(ref _data);

            return 100;
        }

        public int GetMaxHealth()
        {
            if (Definition.BuildableDataDefinition is DestructibleBuildableDataDefinition destructibleDataDefinition)
                return destructibleDataDefinition.MaxHealth;

            return 100;
        }

        public void ApplyDamage(int damage, int tick)
        {
            if (Definition.BuildableDataDefinition is DestructibleBuildableDataDefinition destructibleDataDefinition)
                destructibleDataDefinition.ApplyDamage(ref _data, damage);
        }

        public void SetInteract(bool interact, int tick)
        {
            if (Definition.BuildableDataDefinition is StockpileDataDefinition stockpileDataDefinition)
            {
                stockpileDataDefinition.SetIsInteracting(interact, ref _data);
            }
        }

        public bool GetIsInteracting()
        {
            if (Definition.BuildableDataDefinition is StockpileDataDefinition stockpileDataDefinition)
            {
                return stockpileDataDefinition.GetIsInteracting(ref _data);
            }

            return false;
        }

        public int GetStockpileIndex()
        {
            if (Definition.BuildableDataDefinition is StockpileDataDefinition stockpileDataDefinition)
            {
                return stockpileDataDefinition.GetStockpileIndex(ref _data);
            }

            return -1;
        }

        // Runtime Values

        EBuildableState _currentState;
        int _hitReactTicks = 8;
        int _hitReactEndTick;

        int _destroyedTicks = 64;
        int _destroyedEndTick;

        // Updates on the server if the RuntimePropState is loaded
        // Does not require the monobehaviour to exist
        public bool AuthorityUpdate(int tick)
        {
            if (definitionId == 0)
                return false;

            UpdateState(DataDefinition.GetState(ref _data), tick);

            //Debug.Log(_currentState + ", " + tick);

            switch (_currentState)
            {
                case EBuildableState.Idle:
                    if (GetHealth() == 0)
                    {
                        DataDefinition.SetState(EBuildableState.Destroyed, ref _data);
                    }
                    break;

                case EBuildableState.HitReact:
                    //Debug.Log("buildable HitReact");

                    if (tick > _hitReactEndTick)
                    {
                        DataDefinition.SetState(EBuildableState.Idle, ref _data);
                        return true;
                    }
                    break;
                case EBuildableState.Destroyed:

                    
                    if (tick > _destroyedEndTick)
                    {
                        //Debug.Log("buildable destroyed " + _destroyedEndTick + ", " + tick);
                        DataDefinition.SetState(EBuildableState.Inactive, ref _data);
                        _data.DefinitionID = 0;
                        return true;
                    }
                    break;
            }

            return false;
        }

        private void UpdateState(EBuildableState newState, int tick)
        {
            if (_currentState == newState)
                return;

            _currentState = newState;

           // Debug.Log("State Changed: " + newState);

            switch (_currentState)
            {
                case EBuildableState.Idle:
                    break;
                case EBuildableState.HitReact:
                    _hitReactEndTick = tick + _hitReactTicks;
                    break;
                case EBuildableState.Destroyed:
                    _destroyedEndTick = tick + _destroyedTicks;
                    break;
            }

        }
    }


}