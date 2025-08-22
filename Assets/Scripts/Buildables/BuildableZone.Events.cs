using Fusion;

namespace LichLord.Buildables
{
    public partial class BuildableZone : ContextBehaviour
    {
        [Networked]
        public byte ZoneID { get; set; }

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
            BuildableRuntimeState authorityState = _buildableRuntimeStates[index];

            // Apply the damage
            authorityState.ApplyDamage(damage, tick);

            // Handle destroyed
            if (authorityState.GetState() == EBuildableState.Destroyed)
            {
                int stockpileIndex = authorityState.GetStockpileIndex();
                if(stockpileIndex >= 0) 
                    Context.ContainerManager.ClearStockpile(stockpileIndex);     
            }

            ReplicateRuntimeState(authorityState);
        }

        public void ReplicateRuntimeState(BuildableRuntimeState replictedState)
        {
            ref FBuildableData data = ref _buildableDatas.GetRef(replictedState.index);
            FBuildableData currentData = replictedState.Data;
            data.Copy(ref currentData);
        }
    }
}
