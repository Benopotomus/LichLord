using Fusion;
using UnityEngine;
using System.Collections.Generic;
using LichLord.World;

namespace LichLord.Props
{
    public partial class PropManager : ContextBehaviour
    {
        public void Predict_DealDamageToProp(FChunkPosition chunkPosition, int guid, int damage)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.Predict_ApplyDamageToProp(guid, damage, Runner.Tick);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamageToProp(FChunkPosition chunkPosition, int guid, int damage)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.ApplyDamageToProp(guid, damage, Runner.Tick);
        }
    }
}