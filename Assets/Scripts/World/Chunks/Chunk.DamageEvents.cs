using UnityEngine;
using Fusion;
using System.Collections.Generic;
using LichLord.Props;

namespace LichLord.World
{
    public partial class Chunk
    {
        // This happens on the authority only
        public void ApplyDamageToProp(int guid, int damage, int tick)
        {
            // Find the state
            PropRuntimeState authorityState = _propStates[guid];

            // Apply the damage
            authorityState.ApplyDamage(damage, tick);

            ReplicatePropState(authorityState);
        }

        public void Predict_ApplyDamageToProp(int guid, int damage, int tick)
        {
            if (!PropStates.TryGetValue(guid, out PropRuntimeState authorityState))
            {
                Debug.Log("trying to predict a state that hasn't been loaded");
                return;
            }

            if (_predictedStates.TryGetValue(guid, out var predictedState))
            {
                predictedState.ApplyDamage(damage, tick);
            }
            else
            {
                var newPredictedState = new PropRuntimeState(authorityState);
                newPredictedState.ApplyDamage(damage, tick);

                //Debug.Log("Creating Predicted Data");
                _predictedStates.Add(guid, newPredictedState);
            }
        }
    }
}
