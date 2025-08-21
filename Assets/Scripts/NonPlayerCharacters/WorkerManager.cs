using UnityEngine;
using Fusion;
using LichLord.Buildables;
using System.Runtime.InteropServices;

namespace LichLord.NonPlayerCharacters
{
    public class WorkerManager : ContextBehaviour
    {
        [Networked, Capacity(64)]
        protected NetworkArray<FWorkerData> _workerDatas { get; }

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

            workerData.Zone = zone.Object.Id;
            workerData.BuildableIndex = (ushort)buildableIndex;
            workerData.IsAssigned = true;
        }

        public void AssignWorker(int workerIndex, NonPlayerCharacterReplicator replicator, int npcIndex)
        {
            ref FWorkerData workerData = ref _workerDatas.GetRef(workerIndex);

            workerData.Replicator = replicator.Object.Id;
            workerData.NPCIndex = (byte)npcIndex;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct FWorkerData : INetworkStruct
    {
        [FieldOffset(0)]
        public NetworkId Zone;
        [FieldOffset(4)]
        public ushort BuildableIndex; // 1 byte: NPCState (4 bits)// animation bits
        [FieldOffset(6)]
        public NetworkId Replicator; // 9 bytes: Position (6) + Rotation (2)
        [FieldOffset(10)]
        public byte NPCIndex; // 1 byte: NPCState (4 bits)// animation bits
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
