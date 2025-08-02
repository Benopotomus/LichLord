using Fusion;
using LichLord.Buildables;
using LichLord.Props;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    }

    public class StrongholdManager : ContextBehaviour
    {
        private List<PropRuntimeState> _authorityNexusStates = new List<PropRuntimeState>();
        private List<PropRuntimeState> _predictedStates = new List<PropRuntimeState>();

        public Action<Nexus> onNexusSpawned;
        public Action<Nexus> onNexusDespawned;
        private List<Nexus> _activeNexuses = new List<Nexus>();
        public List<Nexus> ActiveNexuses => _activeNexuses;

        [SerializeField] private Stronghold _strongholdPrefab;

        public override void Spawned()
        {
            base.Spawned();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_ActivatePlayerNexus(FStrongholdData nexusData)
        {
            var position = GetStrongholdPosition(nexusData);

            Runner.Spawn(_strongholdPrefab, position, Quaternion.identity, null,
                                onBeforeSpawned: (runner, obj) =>
                                {
                                    var r = obj.GetComponent<Stronghold>();
                                    r.SetData(nexusData);
                                });

            //Context.InvasionManager.BeginInvasion(1, nexusData);
        }

        public void Predict_ActivatePlayerNexus(FStrongholdData nexusData)
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

        public void OnNexusSpawned(Nexus nexus)
        {
            _activeNexuses.Add(nexus);
            onNexusSpawned?.Invoke(nexus);
        }

        public void OnNexusDespawned(Nexus nexus)
        {
            _activeNexuses.Remove(nexus);
            onNexusDespawned?.Invoke(nexus);
        }

    }

}
