using Fusion;
using LichLord.Props;
using UnityEngine;

namespace LichLord.World
{
    public class ChunkReplicator_128 : ChunkReplicator
    {
        [Networked, Capacity(128)]
        private NetworkArray<FPropData> _propDatas { get; }

        public override void Spawned()
        {
            base.Spawned();
            OnChunkChanged();
            Context.ChunkManager.RegisterReplicator(this);
        }

        protected override void OnChunkChanged()
        {
            base.OnChunkChanged();

            gameObject.name = "Chunk Rep (64):  " + ChunkID.X + ", " + ChunkID.Y;
        }

        protected override void CopyDataFromChunk(Chunk chunk)
        {
            foreach (var deltaStates in chunk.DeltaPropStates)
            {
                ref FPropData propData = ref _propDatas.GetRef(deltaStates.Key);
                propData.Copy(deltaStates.Value);
            }
        }

        public override ref FPropData GetPropData(int index)
        {
            return ref _propDatas.GetRef(index);
        }
    }
}
