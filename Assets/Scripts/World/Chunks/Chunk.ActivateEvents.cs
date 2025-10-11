using UnityEngine;
using LichLord.Props;

namespace LichLord.World
{
    public partial class Chunk
    {
        // This happens on the authority only
        public void SetActivated(int index, bool isInteracting, int tick)
        {
            PropRuntimeState authorityState = _propRuntimeStates[index];
            authorityState.SetActivated(isInteracting, tick);
            ReplicatePropState(authorityState);
        }

        public void Predict_SetActivated(int index, bool isActivated, int tick)
        {
            PropRuntimeState authorityState = _propRuntimeStates[index];

            if (_predictedStates.TryGetValue(index, out var predictedState))
            {
                predictedState.SetActivated(isActivated, tick);
            }
            else
            {
                var newPredictedState = new PropRuntimeState(authorityState);
                newPredictedState.SetActivated(isActivated, tick);
                _predictedStates.Add(index, newPredictedState);
            }
        }
    }
}
