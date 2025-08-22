using UnityEngine;
using Fusion;
using LichLord.Buildables;
using System.Runtime.InteropServices;
using LichLord.World;

namespace LichLord.NonPlayerCharacters
{
    public class WorkerManager : ContextBehaviour
    {
        [Networked, Capacity(64)]
        protected NetworkArray<FWorkerData> _workerDatas { get; }

        protected NonPlayerCharacter[] _workerCharacters = new NonPlayerCharacter[64];

        public override void Spawned()
        {
            base.Spawned();
        }

        public int GetFreeIndex()
        {
            for(int i = 0; i < _workerDatas.Length ; i++) 
            { 
                if(!_workerDatas.Get(i).IsAssigned)
                        return i;
            }

            return -1;
        }

        public void AssignWorkerIndexToBuildable(int workerIndex, BuildableZone zone, int buildableIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);

            workerData.ZoneID = zone.ZoneID;
            workerData.BuildableIndex = (ushort)buildableIndex;
            workerData.IsAssigned = true;
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

                if(buildable is Crypt crypt)
                    return crypt;
            }

            return null;
        }

        public void LoadWorkerData(FWorkerSaveData workerSaveData)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerSaveData.index);
            workerData = workerSaveData.ToNetworkWorker();
            _workerDatas.Set(workerSaveData.index, workerData);
        }

        public void ClearWorker(int workerIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);
            workerData.IsAssigned = false;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FWorkerData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte ZoneID;
        [FieldOffset(1)]
        public ushort BuildableIndex; // 1 byte: NPCState (4 bits)// animation bits
        [FieldOffset(11)]
        private byte _state;

        public bool IsAssigned { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }

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
