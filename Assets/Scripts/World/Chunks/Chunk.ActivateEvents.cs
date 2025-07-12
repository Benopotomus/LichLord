using UnityEngine;
using LichLord.Props;

namespace LichLord.World
{
    public partial class Chunk
    {
        // This happens on the authority only
        public void SetActivated(int guid, bool isInteracting, int tick)
        {
            PropRuntimeState authorityState = _propStates[guid];
            authorityState.SetActivated(isInteracting, tick);
            ReplicatePropState(authorityState);
        }

        public void Predict_SetActivated(int guid, bool isActivated, int tick)
        {
            if (!PropStates.TryGetValue(guid, out PropRuntimeState authorityState))
            {
                Debug.Log("trying to predict a state that hasn't been loaded");
                return;
            }

            if (_predictedStates.TryGetValue(guid, out var predictedState))
            {
                predictedState.SetActivated(isActivated, tick);
            }
            else
            {
                var newPredictedState = new PropRuntimeState(authorityState);
                newPredictedState.SetActivated(isActivated, tick);
                _predictedStates.Add(guid, newPredictedState);
            }
        }
    }
}
