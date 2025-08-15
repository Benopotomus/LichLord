using LichLord.Props;
using System;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableRuntimeState
    {
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

        public BuildableRuntimeState(ref FBuildableData buildableData)
        {
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

            return 0;
        }

        public int GetMaxHealth()
        {
            if (Definition.BuildableDataDefinition is DestructibleBuildableDataDefinition destructibleDataDefinition)
                return destructibleDataDefinition.MaxHealth;

            return 0;
        }

        public void ApplyDamage(int damage, int tick)
        {
            if (Definition.BuildableDataDefinition is DestructibleBuildableDataDefinition destructibleDataDefinition)
                destructibleDataDefinition.ApplyDamage(ref _data, damage);

            _hitReactEndTick = _hitReactTicks + tick;
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

        // Updates on the server if the RuntimePropState is loaded
        // Does not require the monobehaviour to exist
        public bool AuthorityUpdate(int tick)
        {    
            _currentState = DataDefinition.GetState(ref _data);

            switch (_currentState)
            {
                case EBuildableState.HitReact:
                    if (tick > _hitReactEndTick)
                    {
                        _currentState = EBuildableState.Idle;
                        DataDefinition.SetState(_currentState, ref _data);
                        return true;
                    }
                    break;
                case EBuildableState.Destroyed:
                    break;
            }

            return false;
        }
    }
}