using Fusion;
using UnityEngine;
using System.Collections.Generic;

namespace LichLord.Props
{
    public partial class PropManager : ContextBehaviour
    {
        [SerializeField] private Dictionary<int, PropRuntimeState> _localRuntimePropStates = new Dictionary<int, PropRuntimeState>();
        [SerializeField] private Dictionary<int, PropRuntimeState> _predictedStates = new Dictionary<int, PropRuntimeState>();

        public void Predict_DealDamageToProp(int guid, int damage)
        {
            if(!_authorityRuntimePropStates.TryGetValue(guid, out PropRuntimeState authorityState))
            {
                Debug.Log("trying to predict a state that hasn't been loaded");
                return;
            }

            if (_predictedStates.TryGetValue(guid, out var predictedState))
            {
                predictedState.ApplyDamage(damage);
            }
            else
            {
                var newPredictedState= new PropRuntimeState(authorityState);
                newPredictedState.ApplyDamage(damage);
                Debug.Log("Creating Predicted Data: " + newPredictedState.stateData);
                _predictedStates.Add(guid, newPredictedState);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamageToProp(int guid, int damage)
        {
            ApplyDamage(guid, damage);
        }

        public bool GetRenderState(bool hasAuthority, int guid, out PropRuntimeState usedState)
        {
            if (!_authorityRuntimePropStates.TryGetValue(guid, out PropRuntimeState authorityState))
            {
                usedState = null;
                Debug.Log("Get Render State: Trying to get runtime data but its not loaded " + guid);
                return false;
            }

            // Update the authority state if there's a replicated value    
            UpdateRuntimePropState(authorityState);

            usedState = authorityState;

            // If we are the authority, we dont need to handle prediction
            if (hasAuthority)
                return true;

            // Check for predicted data
            bool hasPredictedState = _predictedStates.TryGetValue(guid, out PropRuntimeState predictedState);
            bool hasLocalState = _localRuntimePropStates.TryGetValue(guid, out PropRuntimeState localState);

            // If there's no local state for this guid, create one and return
            if (!hasLocalState)
            { 
                _localRuntimePropStates[guid] = authorityState;
                return true;
            }

            // Check for if local data has changed
            if (localState.stateData != authorityState.stateData)
            {
                // remove predicted states for any changes
                if (hasPredictedState)
                {
                    hasPredictedState = false;
                    _predictedStates.Remove(guid);
                }

                // update the local state data
                localState.stateData = authorityState.stateData;
            }

            // if we still have a predicted state after checking authority changes
            // use that state
            if (hasPredictedState)
            {
                usedState = predictedState;
            }

            return true;
        }

        public void UpdateRuntimePropState(PropRuntimeState runtimeState)
        {
            GetPropReplicationData(runtimeState, out PropReplicator replicator, out FPropData outData);

            if (outData.IsValid())
            {
                runtimeState.guid = outData.GUID;
                runtimeState.definitionId = outData.DefinitionID;
                runtimeState.stateData = outData.StateData;
            }
        }

        public void GetPropReplicationData(PropRuntimeState runtimeState, out PropReplicator replicator, out FPropData outData)
        {
            for (int i = 0; i < _propReplicators.Count; i++)
            {
                PropReplicator propReplicator = _propReplicators[i];

                if (propReplicator.TryGetPropData(runtimeState.guid, out FPropData data))
                {
                    replicator = propReplicator;
                    outData = data;
                    return;
                }
            }

            replicator = null;
            outData = new FPropData();
        }

        public void ApplyDamage(int guid, int damage)
        {
            // Find the state
            PropRuntimeState authorityState = _authorityRuntimePropStates[guid];

            // Apply the damage
            authorityState.ApplyDamage(damage);

            // Update the state in a replicator
            GetPropReplicationData(authorityState, out PropReplicator replicator, out FPropData outData);

            // if we don't have a replicator for that data, find one
            if (replicator == null)
            {
                replicator = GetReplicatorWithFreeSlots();

                // create a new prop data from the state
                outData = new FPropData();
                outData.Copy(authorityState);
            }

            outData.StateData = 1;
            authorityState.stateData = 1;

            replicator.UpdatePropData(guid, outData);

            // Add to the saved states
            _authorityRuntimePropStates[guid] = authorityState;
            _deltaStates[guid] = authorityState;
        }
    }
}