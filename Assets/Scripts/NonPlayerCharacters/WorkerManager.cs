using Fusion;
using LichLord.Buildables;
using System.Runtime.InteropServices;
using LichLord.World;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class WorkerManager : ContextBehaviour
    {
        [Networked, Capacity(BuildableConstants.MAX_WORKERS)]
        protected NetworkArray<FWorkerData> _workerDatas { get; }

        [Networked]
        public int ActiveWorkerCount { get; private set; }

        [Networked]
        public int MaxWorkerCount { get; private set; }

        protected NonPlayerCharacter[] _workerCharacters = new NonPlayerCharacter[BuildableConstants.MAX_WORKERS];

        public override void Spawned()
        {
            base.Spawned();
            MaxWorkerCount = 5; // Set base max worker count to 5
        }

        public ref FWorkerData GetWorkerData(int i)
        {
            return ref _workerDatas.GetRef(i);
        }

        public int GetFreeIndex()
        {
            for (int i = 0; i < _workerDatas.Length; i++)
            {
                if (!_workerDatas.Get(i).IsAssigned)
                    return i;
            }
            return -1;
        }

        public bool AssignWorkerIndexToBuildable(int workerIndex, BuildableZone zone, int buildableIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);

            if (workerData.IsAssigned)
                return false; // Worker already assigned

            workerData.ZoneID = zone.ZoneID;
            workerData.BuildableIndex = (ushort)buildableIndex;
            workerData.IsAssigned = true;

            return true;
        }

        public Crypt GetCrypt(int workerIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);

            BuildableZone zone = Context.StrongholdManager.GetBuildableZone(workerData.ZoneID);

            if (zone == null)
                return null;

            if (zone.LoadStates[workerData.BuildableIndex].LoadState == ELoadState.Loaded)
            {
                Buildable buildable = zone.LoadStates[workerData.BuildableIndex].Buildable;

                if (buildable is Crypt crypt)
                {
                    return crypt;
                }
            }

            return null;
        }

        public void LoadWorkerData(FWorkerSaveData workerSaveData)
        {
            FWorkerData workerData = _workerDatas.GetRef(workerSaveData.index);
            if (workerSaveData.isAssigned)
            {
                workerData = workerSaveData.ToNetworkWorker();
                _workerDatas.Set(workerSaveData.index, workerData);
            }
            else
            {
                workerData.IsAssigned = false;
                _workerDatas.Set(workerSaveData.index, workerData);
            }
        }

        public void AddWorkerCharacter(NonPlayerCharacter character, int workerIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.WorkerActive = true;

            _workerCharacters[workerIndex] = character;

            if(HasStateAuthority)
                ActiveWorkerCount++;
        }

        public void RemoveWorkerCharacter(NonPlayerCharacter character, int workerIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.WorkerActive = false;

            _workerCharacters[workerIndex] = null;

            if (HasStateAuthority)
                ActiveWorkerCount--;
        }

        public bool HasActiveWorker(int workerIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            return workerData.WorkerActive;
        }

        public void ClearWorkerData(int workerIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.IsAssigned = false;
            workerData.WorkerActive = false;
        }

        public void TrySpawnWorker(int workerIndex, NonPlayerCharacterDefinition definition, Vector3 spawnPosition)
        {
            if (HasActiveWorker(workerIndex) ||
                ActiveWorkerCount >= MaxWorkerCount)
            {
                Debug.Log("Cannot spawn. Worker is already active or count is exceeded");
                return;
            }

            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.WorkerActive = true;

            Context.NonPlayerCharacterManager.SpawnNPCWorker(spawnPosition,
                definition,
                ETeamID.PlayerTeam,
                workerIndex);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FWorkerData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte ZoneID;
        [FieldOffset(1)]
        public ushort BuildableIndex;
        [FieldOffset(3)]
        private byte _state;

        public bool IsAssigned { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }
        public bool WorkerActive { get { return IsBitSet(ref _state, 2); } set { SetBit(ref _state, 2, value); } }

        public bool IsBitSet(ref byte flags, int bit)
        {
            return (flags & (1 << bit)) == (1 << bit);
        }

        public byte SetBit(ref byte flags, int bit, bool value)
        {
            if (value == true)
            {
                return flags |= (byte)(1 << bit);
            }
            else
            {
                return flags &= unchecked((byte)~(1 << bit));
            }
        }

        public byte SetBitNoRef(byte flags, int bit, bool value)
        {
            if (value == true)
            {
                return flags |= (byte)(1 << bit);
            }
            else
            {
                return flags &= unchecked((byte)~(1 << bit));
            }
        }
    }
}