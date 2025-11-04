using LichLord.Items;
using LichLord.NonPlayerCharacters;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableRuntimeState
    {
        private int _index; // index in buildable zone
        public int Index => _index;

        private FBuildableData _data = new FBuildableData();
        public FBuildableData Data => _data;

        public BuildableZone buildableZone;
        private SceneContext _context;
        public SceneContext Context => _context;

        private BuildableDefinition _definition;
        public BuildableDefinition Definition
        {
            get
            {
                if (_definition == null)
                    _definition = Global.Tables.BuildableTable.TryGetDefinition(_data.DefinitionID);

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



        public BuildableRuntimeState(BuildableZone zone, int index, ref FBuildableData buildableData)
        {
            this.buildableZone = zone;
            _context = zone.Context;
            this._index = index;
            _data.Copy(in buildableData);
        }

        public void CopyData(ref FBuildableData buildableData)
        {
            if (_data.IsBuildDataEqual(ref buildableData))
                return;

            _data.Copy(in buildableData);
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

            switch (newState)
            {
                case EBuildableState.Destroyed:
                case EBuildableState.Inactive:

                    if (dataDefinition is ContainerDataDefinition containerDataDefinition)
                    {
                        int containerIndex = containerDataDefinition.GetContainerIndex(ref _data);
                        Context.ContainerManager.ClearContainer(containerIndex);
                    }

                    break;

            }
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
                return Definition.MaxHealth;

            return 100;
        }

        public void ApplyDamage(int damage, int tick)
        {
            if (Definition.BuildableDataDefinition is DestructibleBuildableDataDefinition destructibleDataDefinition)
            {
                int currentHealth = GetHealth();
                damage = Mathf.Max(damage - _definition.DamageReduction, 0);
                damage = (int)((float)damage * (1.0f - _definition.DamageResistance));

                destructibleDataDefinition.SetHealth(currentHealth - damage, ref _data);

                if (GetHealth() <= 0)
                {
                    SetState(EBuildableState.Destroyed);
                }
                else
                {
                    SetState(EBuildableState.HitReact);
                }

                if (buildableZone != null) 
                    buildableZone.ReplicateRuntimeState(this);
            }
        }

        // Prioritize destroyed state
        /*
        public EBuildableState TryAssignState(ref FBuildableData buildableData, EBuildableState newState)
        {
            EBuildableState currentState = GetState(ref buildableData);

            switch (newState)
            {
                case EBuildableState.Inactive:
                    SetState(newState, ref buildableData);
                    return newState;
                case EBuildableState.HitReact:
                    switch (currentState)
                    {
                        case EBuildableState.Destroyed:
                        case EBuildableState.Inactive:

                            SetState(currentState, ref buildableData);
                            return currentState;
                    }
                    break;
            }

            SetState(currentState, ref buildableData);
            return newState;
        }
        */

        public void SetInteracting(bool interact, int tick)
        {
            if (_data.DefinitionID == 0)
                return;

            if (Definition.BuildableDataDefinition is ContainerDataDefinition containerDataDefinition)
            {
                containerDataDefinition.SetIsInteracting(interact, ref _data);

                if (buildableZone != null)
                    buildableZone.ReplicateRuntimeState(this);
            }
        }

        public bool GetIsInteracting()
        {
            if (Definition.BuildableDataDefinition is CryptDataDefinition cryptDataDefinition)
            {
                return cryptDataDefinition.GetIsInteracting(ref _data);
            }

            if (Definition.BuildableDataDefinition is ContainerDataDefinition containerDataDefinition)
            {
                return containerDataDefinition.GetIsInteracting(ref _data);
            }

            return false;
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

        public int GetContainerIndex()
        {
            if (Definition.BuildableDataDefinition is ContainerDataDefinition containerData)
            {
                return containerData.GetContainerIndex(ref _data);
            }

            return -1;
        }

        public bool IsRefinery()
        {
            if (Definition is RefineryDefinition refineryDefinition)
            {
                return true;
            }

            return false;
        }

        public ERefineryState GetRefineryState()
        {
            if (Definition.BuildableDataDefinition is RefineryDataDefinition refineryData)
            {
                return refineryData.GetRefineryState(ref _data);
            }

            return ERefineryState.None;
        }

        public void SetRefineryState(ERefineryState newRefineryState)
        {
            if (Definition.BuildableDataDefinition is RefineryDataDefinition refineryData)
            {
                refineryData.SetRefineryState(newRefineryState, ref _data);

                if (buildableZone != null)
                    buildableZone.ReplicateRuntimeState(this);
            }
        }

        public int GetRefineryInSlots()
        {
            if (Definition is RefineryDefinition refineryDefinition)
            {
                return refineryDefinition.InSlots;
            }

            return -1;
        }

        public int GetRefineryOutSlots()
        {
            if (Definition is RefineryDefinition refineryDefinition)
            {
                return refineryDefinition.OutSlots;
            }

            return -1;
        }

        public List<(int, FItemSlotData)> GetRefineryInItemSlotDatas()
        {
            List<(int, FItemSlotData)> inSlots = new List<(int, FItemSlotData)>();

            if (Definition is not RefineryDefinition)
                return inSlots;

            ContainerManager containerManager = _context.ContainerManager;
            FContainerSlotData containerSlotData = containerManager.GetContainerDataAtIndex(GetContainerIndex());

            int startIndex = containerSlotData.StartIndex;
            int inSlotCount = GetRefineryInSlots();

            for (int i = startIndex; i < startIndex + inSlotCount; i++)
            {
                inSlots.Add((i,containerManager.GetItemSlotData(i)));
            }

            return inSlots;
        }

        public List<(int, FItemSlotData)> GetRefineryOutItemSlotDatas()
        {
            List<(int, FItemSlotData)> outSlots = new List<(int, FItemSlotData)>();

            if (Definition is not RefineryDefinition)
                return outSlots;

            ContainerManager containerManager = _context.ContainerManager;
            FContainerSlotData containerSlotData = containerManager.GetContainerDataAtIndex(GetContainerIndex());

            int startIndex = containerSlotData.StartIndex + GetRefineryInSlots();
            int outSlotCount = GetRefineryOutSlots();

            for (int i = startIndex; i < startIndex + outSlotCount; i++)
            {
                outSlots.Add((i, containerManager.GetItemSlotData(i)));
            }

            return outSlots;
        }

        public int GetRefineryProgress()
        {
            if (Definition.BuildableDataDefinition is RefineryDataDefinition refineryData)
            {
                return refineryData.GetRefineryProgress(ref _data);
            }

            return -1;
        }

        public float GetRefineryProgressPercent()
        {
            if (Definition is not RefineryDefinition refineryDefinition)
            {
                return 0;
            }

            if (Definition.BuildableDataDefinition is RefineryDataDefinition refineryData)
            {
                int progress = refineryData.GetRefineryProgress(ref _data);
                int maxProgress = refineryDefinition.MaxProgress;

                return (float)progress / (float)maxProgress;
            }

            return 0;
        }

        public int GetRefineryMaxProgress()
        {
            if (Definition is RefineryDefinition refineryDefinition)
            {
                return refineryDefinition.MaxProgress;
            }

            return -1;
        }

        public void SetRefineryProgress(int newProgress)
        {
            if (Definition.BuildableDataDefinition is RefineryDataDefinition refineryData)
            {
                refineryData.SetRefineryProgress(newProgress, ref _data);

                if (buildableZone != null)
                    buildableZone.ReplicateRuntimeState(this);
            }
        }

        public void SetContainerIndex(int index)
        {
            if (Definition.BuildableDataDefinition is ContainerDataDefinition containerData)
            {
                containerData.SetContainerIndex(index, ref _data);
            }

            if (buildableZone != null)
                buildableZone.ReplicateRuntimeState(this);
        }

        public (int start, int end) GetItemSlotIndexes()
        {
            int fullContainerIndex = GetContainerIndex();

            if (fullContainerIndex == -1)
            { 
                return (-1, -1);
            }

            var containerData = _context.ContainerManager.GetContainerDataAtIndex(fullContainerIndex);

            return (containerData.StartIndex, containerData.EndIndex);
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
            if (_data.DefinitionID == 0)
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