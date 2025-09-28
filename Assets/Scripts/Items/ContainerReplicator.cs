
using Fusion;

namespace LichLord.Items
{
    public class ContainerReplicator : ContextBehaviour
    {
        [Networked]
        public byte Index { get; set; }

        [Networked, Capacity(ItemConstants.CONTAINERS_PER_REPLICATOR)]
        protected virtual NetworkArray<FContainerSlotData> _containerDatas { get; }
        public NetworkArray<FContainerSlotData> ContainerDatas => _containerDatas;

        public override void Spawned()
        {
            base.Spawned();
            Context.ContainerManager.AddContainerReplicator(this);
        }

        public int GetFreeContainerIndex()
        {
            for (int i = 0; i < ItemConstants.CONTAINERS_PER_REPLICATOR; i++)
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

        public void AssignContainerIndex(int index, int startIndex, int endIndex)
        {
            ref FContainerSlotData containerData = ref _containerDatas.GetRef(index);

            containerData.IsAssigned = true;
            containerData.StartIndex = startIndex;
            containerData.EndIndex = endIndex;
        }
    }
}
