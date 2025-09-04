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

        public override void Spawned()
        { 
            base.Spawned();

            OnChunkChanged();
            Context.ChunkManager.RegisterReplicator(this);
        }

        public virtual void OnRender()
        {
            if (!Context.ChunkManager.ChunksReady)
                return;

            if (_lastChunkId.IsEqual(ref ChunkID))
                return;

            _lastChunkId = ChunkID;

            OnChunkChanged();
        }

        protected virtual void OnChunkChanged()
        {
            Chunk oldChunk = Context.ChunkManager.GetChunk(ChunkID);
            if (oldChunk != null)
            {
                oldChunk.ClearReplicator();
            }

            Chunk chunk = Context.ChunkManager.GetChunk(ChunkID);
            if (chunk != null)
            {
                transform.position = chunk.Bounds.center;
                chunk.SetReplicator(this);
                CopyDataFromChunk(chunk);
            }
        }

        public virtual void SetID(FChunkPosition chunkID)
        {
            ChunkID = chunkID;
            OnChunkChanged();
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

        protected virtual void CopyDataFromChunk(Chunk chunk)
        {
        }

        public virtual ref FPropData GetPropData(int index)
        {
            throw new System.NotImplementedException("GetPropData must be overridden in a derived class.");
        }
    }
}
