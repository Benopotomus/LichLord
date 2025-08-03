using Fusion;
using LichLord.Props;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace LichLord.World
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FStrongholdData : INetworkStruct
    {
        [FieldOffset(0)]
        public FChunkPosition ChunkID;
        [FieldOffset(2)]
        public byte GUID;

        public bool IsValid()
        { 
            if(ChunkID.X < 0)
                return false;

            if(ChunkID.Y < 0)
                return false;

            return true;
        }

        public bool IsEqual(FStrongholdData other)
        {
            if (ChunkID.X == other.ChunkID.X &&
                ChunkID.Y == other.ChunkID.Y &&
                GUID == other.GUID)
                return true;

            return false;
        }
    }

    public class StrongholdManager : ContextBehaviour
    {
        private List<PropRuntimeState> _authorityNexusStates = new List<PropRuntimeState>();
        private List<PropRuntimeState> _predictedStates = new List<PropRuntimeState>();

        public Action<Stronghold> onStrongholdSpawned;
        public Action<Stronghold> onStrongholdDespawned;

        [SerializeField] private Stronghold _strongholdPrefab;

        private List<Stronghold> _activeStrongholds = new List<Stronghold>();
        public List<Stronghold> ActiveStrongholds => _activeStrongholds;

        public void OnStrongholdSpawned(Stronghold stronghold)
        { 
            _activeStrongholds.Add(stronghold);
            onStrongholdSpawned?.Invoke(stronghold);
        }

        public void OnStrongholdDespawned(Stronghold stronghold)
        {
            _activeStrongholds.Remove(stronghold);
            onStrongholdDespawned?.Invoke(stronghold);
        }

        public Stronghold GetStronghold(FStrongholdData strongholdData)
        {
            foreach(var stronghold in  _activeStrongholds) 
            {
                if(stronghold.Data.IsEqual(strongholdData))
                    return stronghold;
            }

            return null;
        }


        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_ActivateNexus(FStrongholdData nexusData)
        {
            var position = GetStrongholdPosition(nexusData);

            // Create particle here

            if (HasStateAuthority)
            {
                Runner.Spawn(_strongholdPrefab, position, Quaternion.identity, null,
                                    onBeforeSpawned: (runner, obj) =>
                                    {
                                        var r = obj.GetComponent<Stronghold>();
                                        r.SetData(nexusData, 1000, 1000, 50f);
                                    });
            }
        }

        public void Predict_ActivateNexus(FStrongholdData nexusData)
        {
            _predictedStates.Add(GetNexusState(nexusData));
        }

        // Get the nearest runtime state for a nexus
        public PropRuntimeState GetNearestNexus(Vector3 playerPosition)
        {
            PropRuntimeState nearestNexus = null; // Reset to null each frame
            float minSqrDistance = float.MaxValue;

            List<PropRuntimeState> allNexusStates = new List<PropRuntimeState>(_authorityNexusStates);
            allNexusStates.AddRange(_predictedStates);

            foreach (var state in allNexusStates)
            {
                float sqrDist = Vector3.SqrMagnitude(state.position - playerPosition);
                // Debug.Log($"Nexus {nexusData.GUID} Health: {state.GetHealth()}"); // Uncomment for debugging

                if (sqrDist < minSqrDistance)
                {
                    minSqrDistance = sqrDist;
                    nearestNexus = state;
                }
                
            }

            return nearestNexus;
        }

        public PropRuntimeState GetNexusState(FStrongholdData nexusData)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(nexusData.ChunkID);
            if (chunk != null && chunk.GetRenderState(HasStateAuthority, nexusData.GUID, out var state))
            {
                return state;
            }
        
            return null;
        }

        public Vector3 GetStrongholdPosition(FStrongholdData strongholdData)
        {
            PropRuntimeState nexusState = GetNexusState(strongholdData);
            if (nexusState != null)
                return nexusState.position;

            return Vector3.zero;
        }
    }
}
