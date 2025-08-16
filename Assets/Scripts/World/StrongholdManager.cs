using DWD.Pooling;
using Fusion;
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
        public byte Index;

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
                Index == other.Index)
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
        [SerializeField] private StandaloneVisualEffect _preSpawnVisualEffect;

        private List<Stronghold> _activeStrongholds = new List<Stronghold>();
        public List<Stronghold> ActiveStrongholds => _activeStrongholds;

        public void LoadStrongholds()
        {
            if (HasStateAuthority)
            {
                var loadedStrongholds = Context.WorldSaveLoadManager.LoadedStrongholds;

                foreach (FStrongholdSaveData strongholdSaveData in loadedStrongholds)
                {
                    FStrongholdData strongholdData = new FStrongholdData();
                    strongholdData.ChunkID = strongholdSaveData.chunkCoord;
                    strongholdData.Index = (byte)strongholdSaveData.index;

                    Stronghold strongholdSpawned = SpawnStronghold(strongholdData, strongholdSaveData.currentHealth, strongholdSaveData.rank);

                    strongholdSpawned.BuildableZone.LoadBuildables(strongholdSaveData.buildableStates);
                }
            }
        }

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


        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_ActivateNexus(FStrongholdData strongholdData)
        {
            var position = GetStrongholdPosition(strongholdData);

            // Create particle here
            StandaloneVisualEffect visualEffect = DWDObjectPool.Instance.SpawnAt(_preSpawnVisualEffect, position) as StandaloneVisualEffect;
            visualEffect.Initialize();

            if (HasStateAuthority)
            {
                SpawnStronghold(strongholdData, 1000, 1);
            }
        }

        public Stronghold SpawnStronghold(FStrongholdData strongholdData, int health, int rank)
        {
            var position = GetStrongholdPosition(strongholdData);
            return Runner.Spawn(_strongholdPrefab, position, Quaternion.identity, null,
                                onBeforeSpawned: (runner, obj) =>
                                {
                                    var r = obj.GetComponent<Stronghold>();
                                    r.SetSpawnData(strongholdData, health, rank);
                                });
            
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
            if (chunk != null && chunk.GetRenderState(HasStateAuthority, nexusData.Index, out var state))
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
