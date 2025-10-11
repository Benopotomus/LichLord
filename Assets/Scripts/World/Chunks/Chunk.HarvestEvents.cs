using UnityEngine;
using Fusion;
using System.Collections.Generic;
using LichLord.Props;

namespace LichLord.World
{
    public partial class Chunk
    {
        // This happens on the authority only
        public void HarvestProp(int index, int harvestValue, int tick)
        {
            // Find the state
            PropRuntimeState authorityState = _propRuntimeStates[index];

            // Apply the damage
            authorityState.Harvest(harvestValue, tick);

            ReplicatePropState(authorityState);
        }

        public void Predict_HarvestProp(int index, int harvestValue, int tick)
        {
            PropRuntimeState authorityState = _propRuntimeStates[index];

            if (_predictedStates.TryGetValue(index, out var predictedState))
            {
                predictedState.Harvest(harvestValue, tick);
            }
            else
            {
                var newPredictedState = new PropRuntimeState(authorityState);
                newPredictedState.Harvest(harvestValue, tick);

                //Debug.Log("Creating Predicted Data");
                _predictedStates.Add(index, newPredictedState);
            }
        }
    }
}
