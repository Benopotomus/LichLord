using Fusion;
using LichLord.World;

namespace LichLord.Props
{
    public partial class PropManager : ContextBehaviour
    {
        public void Predict_DealDamage(FChunkPosition chunkPosition, int guid, int damage)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.Predict_ApplyDamageToProp(guid, damage, Runner.Tick);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamage(FChunkPosition chunkPosition, int guid, int damage)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.ApplyDamageToProp(guid, damage, Runner.Tick);
        }

        public void Predict_SetInteracting(FChunkPosition chunkPosition, int guid, bool isInteracting)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.Predict_SetInteracting(guid, isInteracting, Runner.Tick);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SetInteracting(FChunkPosition chunkPosition, int guid, bool isInteracting)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.SetInteracting(guid, isInteracting, Runner.Tick);
        }

        public void Predict_SetActivated(FChunkPosition chunkPosition, int guid, bool isActivated)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.Predict_SetActivated(guid, isActivated, Runner.Tick);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SetActivated(FChunkPosition chunkPosition, int guid, bool isActivated)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.SetActivated(guid, isActivated, Runner.Tick);
        }

        public void Predict_HarvestNode(FChunkPosition chunkPosition, int guid, int harvestValue)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.Predict_HarvestProp(guid, harvestValue, Runner.Tick);
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_HarvestNode(FChunkPosition chunkPosition, int guid, int harvestValue, PlayerCharacter pc)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);
            chunk.HarvestProp(guid, harvestValue, Runner.Tick);

            // Get the Harvest Node GameObject

            if (chunk.PropLoadStates.TryGetValue(guid, out var loadState))
            {
                if (loadState.LoadState == ELoadState.Loaded)
                {
                    if (loadState.Prop is HarvestNode harvestNode)
                    {
                        harvestNode.PlayHarvestParticles(pc);
                    }

                }
            }

        }
    }
}