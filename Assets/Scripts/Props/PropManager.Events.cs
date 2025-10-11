using Fusion;
using LichLord.NonPlayerCharacters;
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
        public void RPC_HarvestNode_PC(FChunkPosition chunkPosition, int guid, int harvestValue, PlayerCharacter pc)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);

            if (HasStateAuthority)
                chunk.HarvestProp(guid, harvestValue, Runner.Tick);

            var loadState = chunk.PropLoadStates[guid];

            if (loadState.LoadState == ELoadState.Loaded)
            {
                if (loadState.Prop is HarvestNode harvestNode)
                {
                    harvestNode.PlayHarvestShake();
                    harvestNode.PlayHarvestParticles(pc.CachedTransform);
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Unreliable, InvokeLocal = true)]
        public void RPC_HarvestProgress_NPC(FChunkPosition chunkPosition, int guid, int harvestValue, NonPlayerCharacterReplicator replicator, byte npcIndex)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);

            var loadState = chunk.PropLoadStates[guid];

            if (loadState.LoadState == ELoadState.Loaded)
            {
                if (loadState.Prop is HarvestNode harvestNode)
                {
                    if (replicator.LoadStates[npcIndex].LoadState == ELoadState.Loaded)
                    {
                        harvestNode.PlayHarvestShake();
                    }
                }
            }
            
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Unreliable, InvokeLocal = true)]
        public void RPC_HarvestNode_NPC(FChunkPosition chunkPosition, int guid, int harvestValue, NonPlayerCharacterReplicator replicator, byte npcIndex)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(chunkPosition);

            if (HasStateAuthority)
                chunk.HarvestProp(guid, harvestValue, Runner.Tick);

            var loadState = chunk.PropLoadStates[guid];
            
            if (loadState.LoadState == ELoadState.Loaded)
            {
                if (loadState.Prop is HarvestNode harvestNode)
                {
                    if (replicator.LoadStates[npcIndex].LoadState == ELoadState.Loaded)
                    {
                        harvestNode.PlayHarvestShake();
                        harvestNode.PlayHarvestParticles(replicator.LoadStates[npcIndex].NPC.CachedTransform);
                    }
                }
            }
        }
    }
}