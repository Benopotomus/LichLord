using Fusion;

namespace LichLord.Buildables
{
    public partial class BuildableZone : ContextBehaviour
    {
        public void Predict_DealDamage(int index, int damage)
        {

        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamage(int index, int damage)
        {
            ApplyDamageToBuildable(index, damage, Runner.Tick);
        }

        // This happens on the authority only
        public void ApplyDamageToBuildable(int index, int damage, int tick)
        {
            // Find the state
            BuildableRuntimeState authorityState = _runtimeStates[index];

            // Apply the damage
            authorityState.ApplyDamage(damage, tick);

            // Handle destroyed
            if (authorityState.GetState() == EBuildableState.Destroyed)
            {
                /*
                int stockpileIndex = authorityState.GetStockpileIndex();
                if(stockpileIndex >= 0) 
                    Context.ContainerManager.ClearStockpile(stockpileIndex);

                
                int workerIndex = authorityState.GetWorkerIndex();
                if (workerIndex >= 0)
                    Context.WorkerManager.ClearWorkerData(workerIndex);
                */

                int containerIndex = authorityState.GetContainerIndex();
                if (containerIndex >= 0)
                    Context.ContainerManager.ClearContainer(containerIndex);
            }
        }

        public void ReplicateRuntimeState(BuildableRuntimeState replictedState)
        {
            ref FBuildableData data = ref _buildableDatas.GetRef(replictedState.Index);
            FBuildableData currentData = replictedState.Data;
            data.Copy(in currentData);
        }
    }
}
