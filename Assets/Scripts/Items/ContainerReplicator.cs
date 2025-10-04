
using Fusion;
using System;

namespace LichLord.Items
{
    public class ContainerReplicator : ContextBehaviour
    {
        [Networked]
        public byte Index { get; set; }

        [Networked, Capacity(ContainerConstants.CONTAINERS_PER_REPLICATOR), OnChangedRender(nameof(OnContainerSlotDataChanged))]
        protected virtual NetworkArray<FContainerSlotData> _containerDatas { get; }
        public NetworkArray<FContainerSlotData> ContainerDatas => _containerDatas;

        private FContainerSlotData[] _localContainerSlotDatas = new FContainerSlotData[ContainerConstants.CONTAINERS_PER_REPLICATOR];

        public Action<int, FContainerSlotData> OnContainerSlotChanged;

        public override void Spawned()
        {
            base.Spawned();
            Context.ContainerManager.AddContainerReplicator(this);
        }

        private void OnContainerSlotDataChanged()
        {
            for (int i = 0; i < ContainerConstants.CONTAINERS_PER_REPLICATOR; i++)
            {
                var networkedSlot = ContainerDatas[i];
                if (!_localContainerSlotDatas[i].IsEqual(networkedSlot))
                {
                    _localContainerSlotDatas[i].Copy(networkedSlot);
                    OnContainerSlotChanged?.Invoke(i + (Index * ContainerConstants.CONTAINERS_PER_REPLICATOR), networkedSlot);
                }
            }
        }

        public int GetFreeContainerIndex()
        {
            for (int i = 0; i < ContainerConstants.CONTAINERS_PER_REPLICATOR; i++)
            {
                ref FContainerSlotData containerData = ref _containerDatas.GetRef(i);
                if (!containerData.IsAssigned) // not taken
                {
                    return i;
                }
            }

            return -1; // no free index found
        }

        public ref FContainerSlotData GetContainerDataAtIndex(int index)
        {
            return ref _containerDatas.GetRef(index);
        }

        public void AssignContainerIndex(int index, int startIndex, int endIndex, bool isStockpile = false)
        {
            ref FContainerSlotData containerData = ref _containerDatas.GetRef(index);

            containerData.IsAssigned = true;
            containerData.IsStockpile = isStockpile;
            containerData.StartIndex = startIndex;
            containerData.EndIndex = endIndex;
        }

        public void ClearContainer(int index)
        {
            ref FContainerSlotData containerData = ref _containerDatas.GetRef(index);
            containerData.IsAssigned = false;
            containerData.IsStockpile = false;
        }
    }
}
