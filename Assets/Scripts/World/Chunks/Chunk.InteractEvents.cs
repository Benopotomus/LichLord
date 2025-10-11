using UnityEngine;
using LichLord.Props;

namespace LichLord.World
{
    public partial class Chunk
    {
        // This happens on the authority only
        public void SetInteracting(int index, bool isInteracting, int tick)
        {
            PropRuntimeState authorityState = _propRuntimeStates[index];
            authorityState.SetInteract(isInteracting, tick);
            ReplicatePropState(authorityState);
        }

        public void Predict_SetInteracting(int index, bool isInteracting, int tick)
        {
            PropRuntimeState authorityState = _propRuntimeStates[index];

            if (_predictedStates.TryGetValue(index, out var predictedState))
            {
                predictedState.SetInteract(isInteracting, tick);
            }
            else
            {
                var newPredictedState = new PropRuntimeState(authorityState);
                newPredictedState.SetInteract(isInteracting, tick);
                _predictedStates.Add(index, newPredictedState);
            }
        }
    }
}
