using LichLord.NonPlayerCharacters;
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
        public BuildableZone buildableZone;

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

        public BuildableRuntimeState(BuildableZone zone, int index, ref FBuildableData buildableData)
        {
            this.buildableZone = zone;
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

        public void SetState(EBuildableState newState)
        {
            BuildableDataDefinition dataDefinition = Definition.BuildableDataDefinition;
            dataDefinition.SetState(newState, ref _data);

            if (buildableZone != null)
                buildableZone.ReplicateRuntimeState(this);
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
            {
                destructibleDataDefinition.ApplyDamage(ref _data, damage);

                if(buildableZone != null) 
                    buildableZone.ReplicateRuntimeState(this);
            }
        }

        public void SetInteracting(bool interact, int tick)
        {
            if (Definition.BuildableDataDefinition is StockpileDataDefinition stockpileDataDefinition)
            {
                stockpileDataDefinition.SetIsInteracting(interact, ref _data);

                if (buildableZone != null)
                    buildableZone.ReplicateRuntimeState(this);
            }
        }

        public bool GetIsInteracting()
        {
            if (Definition.BuildableDataDefinition is StockpileDataDefinition stockpileDataDefinition)
            {
                return stockpileDataDefinition.GetIsInteracting(ref _data);
            }

            if (Definition.BuildableDataDefinition is CryptDataDefinition cryptDataDefinition)
            {
                return cryptDataDefinition.GetIsInteracting(ref _data);
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

        public int GetWorkerIndex()
        {
            if (Definition.BuildableDataDefinition is CryptDataDefinition cryptDataDefinition)
            {
                return cryptDataDefinition.GetWorkerIndex(ref _data);
            }

            return -1;
        }

        public EWorkerState GetWorkerState()
        {
            if (Definition.BuildableDataDefinition is CryptDataDefinition cryptDataDefinition)
            {
                return cryptDataDefinition.GetWorkerState(ref _data);
            }

            return EWorkerState.None;
        }

        public void SetWorkerState(EWorkerState newState)
        {
            if (Definition.BuildableDataDefinition is CryptDataDefinition cryptDataDefinition)
            {
                cryptDataDefinition.SetWorkerState(newState, ref _data);

                if (buildableZone != null)
                    buildableZone.ReplicateRuntimeState(this);
            }
        }

        public int GetWorkerSpawnTicks()
        {
            if (Definition.BuildableDataDefinition is CryptDataDefinition cryptDataDefinition)
            {
                return cryptDataDefinition.WorkerRespawnTicks;
            }

            return -1;
        }

        public NonPlayerCharacterDefinition GetWorkerDefinition()
        {
            if (Definition.BuildableDataDefinition is CryptDataDefinition cryptDataDefinition)
            {
                return cryptDataDefinition.WorkerDefinition;
            }

            return null;
        }

        // Runtime Values

        EBuildableState _currentState;
        int _hitReactTicks = 8;
        int _hitReactEndTick;

        int _destroyedTicks = 64;
        int _destroyedEndTick;

        // Updates on the server if the RuntimePropState is loaded
        // Does not require the monobehaviour to exist
        // Ticks slowly
        public bool AuthorityUpdateTick(int tick)
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
                        SetState(EBuildableState.Destroyed);
                        return true;
                    }
                    break;

                case EBuildableState.HitReact:
                    //Debug.Log("buildable HitReact");

                    if (tick > _hitReactEndTick)
                    {
                        SetState(EBuildableState.Idle);
                        return true;
                    }
                    break;
                case EBuildableState.Destroyed:

                    if (tick > _destroyedEndTick)
                    {
                        _data.DefinitionID = 0;
                        SetState(EBuildableState.Inactive);
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