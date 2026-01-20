using Fusion;

namespace LichLord.NonPlayerCharacters
{
    public partial class NonPlayerCharacterReplicator : ContextBehaviour
    {
        public void Predict_DealDamageToNPC(int index, int damage, int hitReactIndex, int additiveHitReactIndex)
        {
            var targetData = _npcDatas.Get(index);

            int predictionTicks = 32;

            if (_predictedStates.TryGetValue(index, out NonPlayerCharacterRuntimeState predictedState))
            {
                predictedState.ApplyDamage(damage, hitReactIndex, additiveHitReactIndex);
                predictedState.PredictionStartTick = Runner.Tick + 0;
                predictedState.PredictionTimeoutTick = Runner.Tick + predictionTicks;
            }
            else
            {
                int fullIndex = index + (NonPlayerCharacterConstants.MAX_NPC_REPS * Index);
                NonPlayerCharacterRuntimeState newPredictedState = new NonPlayerCharacterRuntimeState(this, index, fullIndex);
                newPredictedState.CopyData(ref targetData);
                newPredictedState.ApplyDamage(damage, hitReactIndex, additiveHitReactIndex);
                newPredictedState.PredictionStartTick = Runner.Tick + 0;
                newPredictedState.PredictionTimeoutTick = Runner.Tick + predictionTicks;
                _predictedStates[index] = newPredictedState;

                //Debug.Log("Predicted State " + newPredictedState.GetState() + "Anim: " + newPredictedState.GetAnimationIndex());
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamageToNPC(int index, int damage, int hitReactIndex, int additiveHitReactIndex)
        {
            if (_loadStates[index].LoadState == ELoadState.Loaded)
            {
                //_loadStates[index].NPC.HitReact.StartAdditiveHitReact();
            }

            _localRuntimeStates[index].ApplyDamage(damage, hitReactIndex, additiveHitReactIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SetNPCState(int index, ENPCState newState)
        {
            _localRuntimeStates[index].SetState(newState);
        }
    }
}
