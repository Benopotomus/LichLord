using Fusion;
using LichLord.Props;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.World
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FNexusData : INetworkStruct
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

    public class NexusManager : ContextBehaviour
    {
        [Networked, Capacity(16)]
        private NetworkArray<FNexusData> _nexusDatas { get; }
        
        [Networked]
        private int _dataCount { get; set; }

        private List<PropRuntimeState> _authorityStates = new List<PropRuntimeState>();
        private List<PropRuntimeState> _predictedStates = new List<PropRuntimeState>();

        protected ArrayReader<FNexusData> _dataBufferReader;
        protected PropertyReader<int> _dataCountReader;

        protected int _viewCount;

        public override void Spawned()
        {
            base.Spawned();
            _dataBufferReader = GetArrayReader<FNexusData>(nameof(_nexusDatas));
            _dataCountReader = GetPropertyReader<int>(nameof(_dataCount));

            _viewCount = _dataCount;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_AddNexus(FNexusData nexusData)
        {
            _nexusDatas.Set(_dataCount, nexusData);
            _dataCount++;

            Context.InvasionManager.BeginInvasion(1, nexusData);
        }

        public void Predict_AddNexus(FNexusData nexusData)
        {
            _predictedStates.Add(GetNexusState(nexusData));
        }

        public override void Render()
        {
            base.Render();

            if (!Context.IsGameplayActive())
                return;

            if (TryGetSnapshotsBuffers(out var fromNetworkBuffer, out var toNetworkBuffer, out float bufferAlpha) == false)
                return;

            int fromDataCount = _dataCountReader.Read(fromNetworkBuffer);
            int toDataCount = _dataCountReader.Read(toNetworkBuffer);

            NetworkArrayReadOnly<FNexusData> fromDataBuffer = _dataBufferReader.Read(fromNetworkBuffer);
            NetworkArrayReadOnly<FNexusData> toDataBuffer = _dataBufferReader.Read(toNetworkBuffer);

            // Spawn missing views
            for (int i = _viewCount; i < fromDataCount; i++)
            {
                var nexusState = GetNexusState(toDataBuffer[i]);
                _authorityStates.Add(nexusState);

                if (_predictedStates.Contains(nexusState))
                    _predictedStates.Remove(nexusState);
                    return;

            }

            _viewCount = fromDataCount;
        }

        // Get the nearest runtime state for a nexus
        public PropRuntimeState GetNearestNexus(Vector3 playerPosition)
        {
            PropRuntimeState nearestNexus = null; // Reset to null each frame
            float minSqrDistance = float.MaxValue;

            List<PropRuntimeState> allNexusStates = new List<PropRuntimeState>(_authorityStates);
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

        public PropRuntimeState GetNexusState(FNexusData nexusData)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(nexusData.ChunkID);
            if (chunk != null && chunk.GetRenderState(HasStateAuthority, nexusData.GUID, out var state))
            {
                return state;
            }
        
            return null;
        }

    }

}
