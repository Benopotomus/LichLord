using Fusion;
using LichLord.Props;
using LichLord.World;
using Mono.Cecil.Cil;
using UnityEngine;

namespace LichLord.World
{
    public class ChunkReplicator : ContextBehaviour
    {
        [Networked]
        public FChunkPosition ChunkID { get; private set; }

        [Networked, Capacity(PropConstants.MAX_PROP_REPS)]
        private NetworkArray<FPropData> _propDatas { get; }

        [SerializeField]
        private Chunk chunk;

        public override void Spawned()
        { 
            base.Spawned();
            OnSpawned();

            if (HasStateAuthority)
            {
                Context.ChunkManager.RegisterReplicator(this);
            }
        }

        public void OnSpawned()
        {
            chunk = Context.ChunkManager.GetChunk(ChunkID);
            transform.position = chunk.Bounds.center;
            gameObject.name = "Chunk Rep: " + ChunkID.X + ", " + ChunkID.Y;
            chunk.SetReplicator(this);
            CopyDataFromChunk();
        }

        public void PreSpawned(FChunkPosition chunkID)
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
            Context.ChunkManager.UnregisterReplicator(this);
            chunk.SetReplicator(this);
        }

        private void CopyDataFromChunk()
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
