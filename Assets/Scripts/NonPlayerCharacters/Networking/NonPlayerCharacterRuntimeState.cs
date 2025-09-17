using LichLord.Dialog;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterRuntimeState
    {
        public int PredictionTimeoutTick; // Max lifetime of predictive state

        private int _index;
        public int Index => _index;

        private NonPlayerCharacterReplicator _replicator;

        private SceneContext _context;
        public SceneContext Context => _context;

        FNonPlayerCharacterData _data = new FNonPlayerCharacterData();
        public FNonPlayerCharacterData Data => _data;

        private NonPlayerCharacterDefinition _definition;
        public NonPlayerCharacterDefinition Definition
        {
            get
            {
                if (_definition == null)
                    _definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(_data.DefinitionID);

                return _definition;
            }
        }

        private NonPlayerCharacterDataDefinition _dataDefinition;
        public NonPlayerCharacterDataDefinition DataDefinition
        {
            get
            {
                if (_data.DefinitionID == 0)
                    return null;

                if (_dataDefinition == null)
                    _dataDefinition = _data.DataDefinition;

                return _dataDefinition;
            }
        }

        public NonPlayerCharacterRuntimeState(NonPlayerCharacterReplicator replicator, int index)
        {
            _replicator = replicator;
            _context = replicator.Context;
            _index = index;
        }

        public void CopyData(ref FNonPlayerCharacterData other)
        { 
            _data.Copy(ref other);

            if (_data.DefinitionID == 0)
                return;

            _definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(_data.DefinitionID);
            _dataDefinition = _data.DataDefinition;
        }

        public void ApplyDamage(int damage, int hitReactIndex)
        {
            DataDefinition.ApplyDamage(ref _data, damage, hitReactIndex);
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
            return DataDefinition.GetTeamID(ref _data);
        }

        public ENPCSpawnType GetSpawnType()
        {
            return _data.SpawnType;
        }

        public EAttitude GetAttitude()
        {
            return DataDefinition.GetAttitude(ref _data);
        }

        public void SetAttitude(EAttitude newAttitude)
        {
            if (_data.DefinitionID == 0)
                return;

            DataDefinition.SetAttitude(newAttitude, ref _data);
            _replicator.ReplicateRuntimeState(this);
        }

        public ENPCState GetState()
        {
            if(_data.DefinitionID == 0)
                return ENPCState.Inactive;

            if (DataDefinition == null)
                return ENPCState.Inactive;

            return DataDefinition.GetState(ref _data);
        }

        public void SetState(ENPCState newState)
        {
            if (_data.DefinitionID == 0)
                return;

            DataDefinition.SetState(newState, ref _data);
            _replicator.ReplicateRuntimeState(this);
        }

        public int GetAnimationIndex()
        {
            return DataDefinition.GetAnimationIndex(ref _data);
        }

        public void SetAnimationIndex(int index)
        {
            DataDefinition.SetAnimationIndex(index, ref _data);
            _replicator.ReplicateRuntimeState(this);
        }

        public Vector3 GetPosition()
        {
            return _data.Position;
        }

        public void SetPosition(Vector3 position)
        { 
            _data.Position = position;
        }

        public Quaternion GetRotation()
        {
            return _data.Rotation;
        }

        public float GetYaw()
        {
            return _data.Yaw;
        }

        public byte GetRawCompressedYaw()
        {
            return _data.RawCompressedYaw;
        }

        public int GetTargetPlayerIndex()
        { 
            return _data.TargetPlayerIndex; 
        }

        public int GetHealth()
        { 
            return DataDefinition.GetHealth(ref _data);
        }

        public int GetMaxHealth()
        {
            return Definition.MaxHealth;
        }

        public bool IsInvader()
        {
            if (NonPlayerCharacterDataUtility.GetSpawnType(ref _data) == ENPCSpawnType.Invader)
                return true;

            return false;
        }

        public bool IsWarrior()
        {
            if (NonPlayerCharacterDataUtility.GetSpawnType(ref _data) == ENPCSpawnType.Warrior)
                return true;

            return false;
        }

        public int GetFormationID()
        {
            if (DataDefinition is WarriorDataDefinition warriorDataDefinition)
            {
                return warriorDataDefinition.GetFormationID(ref _data);
            }

            return -1;
        }

        public int GetFormationIndex()
        {
            if (DataDefinition is WarriorDataDefinition warriorDataDefinition)
            {
                return warriorDataDefinition.GetFormationIndex(ref _data);
            }

            return -1;
        }

        public Vector3 GetInvaderFormationOffset()
        {
            if (DataDefinition is InvaderDataDefinition invaderDataDefinition)
                return invaderDataDefinition.GetFormationOffset(ref _data);

            return Vector3.zero;
        }

        public ENPCState GetStateFromData(ref FNonPlayerCharacterData otherData)
        {
            if (_data.DefinitionID == 0)
                return ENPCState.Inactive;

            if (DataDefinition == null)
                return ENPCState.Inactive;

            return DataDefinition.GetState(ref otherData);
        }

        // Worker

        public bool IsWorker()
        {
            if (NonPlayerCharacterDataUtility.GetSpawnType(ref _data) == ENPCSpawnType.Worker)
                return true;

            return false;
        }

        public int GetWorkerIndex()
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
                return workerDataDefinition.GetWorkerIndex(ref _data);

            return -1;
        }

        // Harvest

        public ECurrencyType GetCarriedCurrencyType()
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
                return workerDataDefinition.GetCurrencyType(ref _data);

            return ECurrencyType.None;
        }

        public void SetCarriedCurrencyType(ECurrencyType newCurrencyType)
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
            {
                workerDataDefinition.SetCurrencyType(newCurrencyType, ref _data);
                _replicator.ReplicateRuntimeState(this);
            }
        }

        public int GetCarriedCurrencyAmount()
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
            {
                var currencyType = workerDataDefinition.GetCurrencyType(ref _data);
                return Definition.GetCarryValue(currencyType);
            }

            return 0;
        }

        public int GetHarvestProgress()
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
                return workerDataDefinition.GetHarvestProgress(ref _data);

            return 0;
        }

        public void SetHarvestProgress(int newStacks)
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
            {
                workerDataDefinition.SetHarvestProgress(newStacks, ref _data);
                _replicator.ReplicateRuntimeState(this);
            }
        }

        public void AddHarvestProgress(int newStacks)
        {
            if (DataDefinition is WorkerDataDefinition workerDataDefinition)
            {
                int oldStacks = workerDataDefinition.GetHarvestProgress(ref _data);

                workerDataDefinition.SetHarvestProgress(oldStacks + newStacks, ref _data);
                _replicator.ReplicateRuntimeState(this);
            }
        }

        // Dialog

        public int GetDialogIndex()
        {
            return DataDefinition.GetDialogIndex(ref _data);
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
            return DataDefinition.HasDialog(ref _data);
        }

        // Player Follow

        public PlayerCharacter GetFollowPlayer()
        {
            if (DataDefinition is WarriorDataDefinition warriorData)
            {
                return Context.NetworkGame.GetPlayerByIndex(warriorData.GetPlayerFollowIndex(ref _data));
            }

            return null;
        }

        // Lifetime

        public int GetLifetimeProgress()
        {
            if (DataDefinition is WarriorDataDefinition warriorData)
            {
                return warriorData.GetLifetimeProgress(ref _data);
            }

            return 0;
        }

        public void SetLifetimeProgress(int newProgress)
        {
            if (DataDefinition is WarriorDataDefinition warriorData)
            {
                warriorData.SetLifetimeProgress(newProgress, ref _data);
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
