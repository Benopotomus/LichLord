using Fusion;
using LichLord.Props;
using UnityEngine;

namespace LichLord.World
{
    public class ChunkReplicator : ContextBehaviour
    {
        [Networked]
        public ref FChunkPosition ChunkID => ref MakeRef<FChunkPosition>();
        private FChunkPosition _lastChunkId;

        [Networked, Capacity(PropConstants.MAX_PROP_REPS)]
        private NetworkArray<FPropData> _propDatas { get; }

        public override void Spawned()
        { 
            base.Spawned();
            OnChunkChanged();
            Context.ChunkManager.RegisterReplicator(this);
        }

        public void OnRender()
        {
            if (_lastChunkId.IsEqual(ref ChunkID))
                return;

            _lastChunkId = ChunkID;

            OnChunkChanged();
        }

        private void OnChunkChanged()
        {
            Chunk chunk = Context.ChunkManager.GetChunk(ChunkID);
            transform.position = chunk.Bounds.center;
            gameObject.name = "Chunk Rep: " + ChunkID.X + ", " + ChunkID.Y;
            chunk.Replicator = this;
            CopyDataFromChunk(chunk);
        }

        public void SetID(FChunkPosition chunkID)
        {
            ChunkID = chunkID;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            if (hasState && HasStateAuthority)
            {
                Deactivate();
            }
        }

        public void Deactivate()
        {

        }

        private void CopyDataFromChunk(Chunk chunk)
        {
            foreach (var deltaStates in chunk.DeltaPropStates)
            {
                ref FPropData propData = ref _propDatas.GetRef(deltaStates.Key);
                propData.Copy(deltaStates.Value);
            }
        }

        public ref FPropData GetPropData(int index)
        { 
            return ref _propDatas.GetRef(index);
        }
    }
}
