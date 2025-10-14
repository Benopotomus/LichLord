using LichLord.Buildables;
using LichLord.Dialog;
using LichLord.Items;
using LichLord.Props;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterRuntimeState
    {
        public int PredictionTimeoutTick; // Max lifetime of predictive state

        private int _localIndex;
        public int LocalIndex => _localIndex;

        private int _fullIndex;
        public int FullIndex => _fullIndex;

        private NonPlayerCharacterReplicator _replicator;

        private SceneContext _context;
        public SceneContext Context => _context;

        FNonPlayerCharacterData _npcData = new FNonPlayerCharacterData();
        public FNonPlayerCharacterData Data => _npcData;

        private NonPlayerCharacterDefinition _definition;
        public NonPlayerCharacterDefinition Definition
        {
            get
            {
                if (_definition == null)
                    _definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(_npcData.DefinitionID);

                return _definition;
            }
        }

        private NonPlayerCharacterDataDefinition _dataDefinition;
        public NonPlayerCharacterDataDefinition DataDefinition
        {
            get
            {
                if (_npcData.DefinitionID == 0)
                    return null;

                if (_dataDefinition == null)
                    _dataDefinition = _npcData.DataDefinition;

                return _dataDefinition;
            }
        }

        public NonPlayerCharacterRuntimeState(NonPlayerCharacterReplicator replicator, int localIndex, int fullIndex)
        {
            _replicator = replicator;
            _context = replicator.Context;
            _localIndex = localIndex;
            _fullIndex = fullIndex;
        }

        public void CopyData(ref FNonPlayerCharacterData other)
        { 
            _npcData.Copy(ref other);

            if (_npcData.DefinitionID == 0)
                return;

            _definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(_npcData.DefinitionID);
            _dataDefinition = _npcData.DataDefinition;
        }

        public void ApplyDamage(int damage, int hitReactIndex)
        {
            DataDefinition.ApplyDamage(ref _npcData, damage, hitReactIndex);
            _replicator.ReplicateRuntimeState(this);

            if (IsInvader())
            {
                InvasionManager invasionManager = Context.InvasionManager;
                if (invasionManager.HasStateAuthority)
                {
                    if (invasionManager.InvasionState == EInvasionState.Approaching)
                    {
                        if (GetAttitude() == EAttitude.Defensive)
                        {
                            invasionManager.RPC_SetInvadersHostile();
                        }
                    }
                }
            }
        }

        public bool IsActive()
        {
            return GetState() != ENPCState.Inactive;
        }

        public ETeamID GetTeam()
        { 
            return DataDefinition.GetTeamID(ref _npcData);
        }

        public ENPCSpawnType GetSpawnType()
        {
            return _npcData.SpawnType;
        }

        public EAttitude GetAttitude()
        {
            return DataDefinition.GetAttitude(ref _npcData);
        }

        public void SetAttitude(EAttitude newAttitude)
        {
            if (_npcData.DefinitionID == 0)
                return;

            DataDefinition.SetAttitude(newAttitude, ref _npcData);
            _replicator.ReplicateRuntimeState(this);
        }

        public ENPCState GetState()
        {
            if(_npcData.DefinitionID == 0)
                return ENPCState.Inactive;

            if (DataDefinition == null)
                return ENPCState.Inactive;

            return DataDefinition.GetState(ref _npcData);
        }

        public void SetState(ENPCState newState)
        {
            if (_npcData.DefinitionID == 0)
                return;

            DataDefinition.SetState(newState, ref _npcData);
            _replicator.ReplicateRuntimeState(this);
        }

        public int GetAnimationIndex()
        {
            return DataDefinition.GetAnimationIndex(ref _npcData);
        }

        public void SetAnimationIndex(int index)
        {
            DataDefinition.SetAnimationIndex(index, ref _npcData);
            _replicator.ReplicateRuntimeState(this);
        }

        public Vector3 GetPosition()
        {
            return _npcData.Position;
        }

        public void SetPosition(Vector3 position)
        { 
            _npcData.Position = position;
        }

        public Quaternion GetRotation()
        {
            return _npcData.Rotation;
        }

        public float GetYaw()
        {
            return _npcData.Yaw;
        }

        public byte GetRawCompressedYaw()
        {
            return _npcData.RawCompressedYaw;
        }

        public int GetTargetPlayerIndex()
        { 
            return _npcData.TargetPlayerIndex; 
        }

        public int GetHealth()
        { 
            return DataDefinition.GetHealth(ref _npcData);
        }

        public int GetMaxHealth()
        {
            return Definition.MaxHealth;
        }

        public bool IsInvader()
        {
            if (NonPlayerCharacterDataUtility.GetSpawnType(ref _npcData) == ENPCSpawnType.Invader)
                return true;

            return false;
        }

        public bool IsWarrior()
        {
            if (NonPlayerCharacterDataUtility.GetSpawnType(ref _npcData) == ENPCSpawnType.Warrior)
                return true;

            return false;
        }

        public int GetFormationID()
        {
            if (DataDefinition is WarriorDataDefinition warriorDataDefinition)
            {
                return warriorDataDefinition.GetFormationID(ref _npcData);
            }

            return -1;
        }

        public int GetFormationIndex()
        {
            if (DataDefinition is WarriorDataDefinition warriorDataDefinition)
            {
                return warriorDataDefinition.GetFormationIndex(ref _npcData);
            }

            return -1;
        }

        public Vector3 GetInvaderFormationOffset()
        {
            if (DataDefinition is InvaderDataDefinition invaderDataDefinition)
                return invaderDataDefinition.GetFormationOffset(ref _npcData);

            return Vector3.zero;
        }

        public ENPCState GetStateFromData(ref FNonPlayerCharacterData otherData)
        {
            if (_npcData.DefinitionID == 0)
                return ENPCState.Inactive;

            if (DataDefinition == null)
                return ENPCState.Inactive;

            return DataDefinition.GetState(ref otherData);
        }

        // Worker

        public bool IsWorker()
        {
            if (NonPlayerCharacterDataUtility.GetSpawnType(ref _npcData) == ENPCSpawnType.Worker)
                return true;

            return false;
        }

        public bool IsWorkerValid()
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
                return workerDataDefinition.IsValid(ref _npcData);

            return false;
        }

        public int GetWorkerStrongholdId()
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
                return workerDataDefinition.GetStrongholdId(ref _npcData);

            return -1;
        }

        public int GetWorkerIndex()
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
                return workerDataDefinition.GetWorkerIndex(ref _npcData);

            return -1;
        }

        public void InvalidateWorker()
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
            {
                workerDataDefinition.SetInvalid(ref _npcData);
                _replicator.ReplicateRuntimeState(this);
            }
        }

        public Stronghold GetWorkerStronghold()
        {
            if (!IsWorker())
                return null;

            return Context.StrongholdManager.GetStronghold(GetWorkerStrongholdId());
        }

        public void SendWorkerStateChanged(ENPCState newState)
        {
            if (!IsWorker() || !IsWorkerValid())
                return;

            Stronghold stronghold = Context.StrongholdManager.GetStronghold(GetWorkerStrongholdId());
            stronghold.WorkerComponent.OnWorkerStateChanged(GetWorkerIndex(), newState);
        }

        public CommandTaskDefinition[] GetCommandTasks()
        {
            if (DataDefinition is not WorkerDataDefinition workerDataDefinition)
                return new CommandTaskDefinition[0];

            return Definition.CommandTasks;
        }

        public bool IsHarvestNodeValid(PropRuntimeState runtimeState)
        {
            if (!IsWorker())
                return false;

            Stronghold stronghold = GetWorkerStronghold();
            var workerData = stronghold.WorkerComponent.GetWorkerData(GetWorkerIndex());
            var tasks = GetCommandTasks();

            for (int i = 0; i < tasks.Length; i++)
            {
                bool isActive = workerData.TasksData.IsTaskActive(i);

                if(!isActive)
                    continue;

                var task = tasks[i];

                if (task.TaskType == runtimeState.GetValidTaskType())
                    return true;
            }

            return false;

        }

        // Harvest

        public FItemData GetCarriedItem()
        {
            if (_npcData.DefinitionID == 0)
                return new FItemData();

            return _dataDefinition.GetCarriedItem(ref _npcData);
        }

        public void SetCarriedItem(FItemData newItem)
        {
            DataDefinition.SetCarriedItem( newItem, ref _npcData);
        }

        public int GetHarvestProgress()
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
                return workerDataDefinition.GetHarvestProgress(ref _npcData);

            return 0;
        }

        public void SetHarvestProgress(int newStacks)
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
            {
                workerDataDefinition.SetHarvestProgress(newStacks, ref _npcData);
                _replicator.ReplicateRuntimeState(this);
            }
        }

        public void AddHarvestProgress(int newStacks)
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
            {
                int oldStacks = workerDataDefinition.GetHarvestProgress(ref _npcData);

                workerDataDefinition.SetHarvestProgress(oldStacks + newStacks, ref _npcData);
                _replicator.ReplicateRuntimeState(this);
            }
        }

        // Dialog

        public int GetDialogIndex()
        {
            return DataDefinition.GetDialogIndex(ref _npcData);
        }

        public DialogDefinition GetDialogDefinition()
        {
            int dialogIndex = GetDialogIndex();
            if (dialogIndex < 0)
                return null;

            return _replicator.Context.DialogManager.GetDialogDefinition(dialogIndex);
        }

        public bool HasDialog()
        { 
            return DataDefinition.HasDialog(ref _npcData);
        }

        // Player Follow

        public PlayerCharacter GetFollowPlayer()
        {
            if (DataDefinition is WarriorDataDefinition warriorData)
            {
                return Context.NetworkGame.GetPlayerByIndex(warriorData.GetPlayerFollowIndex(ref _npcData));
            }

            return null;
        }

        // Lifetime

        public int GetLifetimeProgress()
        {
            if (DataDefinition is WarriorDataDefinition warriorData)
            {
                return warriorData.GetLifetimeProgress(ref _npcData);
            }

            return 0;
        }

        public void SetLifetimeProgress(int newProgress)
        {
            if (DataDefinition is WarriorDataDefinition warriorData)
            {
                warriorData.SetLifetimeProgress(newProgress, ref _npcData);
                _replicator.ReplicateRuntimeState(this);
            }
        }

        public int GetTicksPerLifetime()
        {
            if (DataDefinition is WarriorDataDefinition warriorData)
            {
                return warriorData.TicksPerLifetimeProgress;
            }

            return -1;
        }

        public int GetLifetimeProgressMax()
        {
            if (DataDefinition is WarriorDataDefinition warriorData)
            {
                return warriorData.MaxLifetimeProgress;
            }

            return -1;
        }
    }
}
