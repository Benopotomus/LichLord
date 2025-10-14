using DWD.Pooling;
using Fusion;
using LichLord.Buildables;
using LichLord.Items;
using LichLord.Props;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.World
{
    public class StrongholdManager : ContextBehaviour
    {
        private List<PropRuntimeState> _authorityNexusStates = new List<PropRuntimeState>();
        private List<PropRuntimeState> _predictedStates = new List<PropRuntimeState>();

        public Action<Stronghold> onStrongholdSpawned;
        public Action<Stronghold> onStrongholdDespawned;

        [SerializeField] private Stronghold _strongholdPrefab;
        [SerializeField] private StandaloneVisualEffect _preSpawnVisualEffect;

        [SerializeField]
        private List<Stronghold> _activeStrongholds = new List<Stronghold>();
        public List<Stronghold> ActiveStrongholds => _activeStrongholds;

        public void LoadStrongholds()
        {
            if (!HasStateAuthority)
                return;

            var loadedStrongholds = Context.WorldSaveLoadManager.LoadedStrongholds;

            foreach (FStrongholdSaveData strongholdSaveData in loadedStrongholds)
            {
                FStaticPropPosition propPosition  = new FStaticPropPosition();
                propPosition.ChunkPosition = strongholdSaveData.chunkCoord;
                propPosition.PropIndex = (ushort)strongholdSaveData.index;

                Stronghold strongholdSpawned = SpawnStronghold(strongholdSaveData.strongholdId, 
                    propPosition, 
                    strongholdSaveData.currentHealth, 
                    strongholdSaveData.rank,
                    strongholdSaveData.containerIndex);

                strongholdSpawned.BuildableZone.LoadBuildables(strongholdSaveData.buildableStates);
                strongholdSpawned.WorkerComponent.LoadWorkerData(strongholdSaveData.workerSaveDatas);
            }
        }

        public void OnStrongholdSpawned(Stronghold stronghold)
        {
            if (_activeStrongholds.Contains(stronghold))
                return;

            _activeStrongholds.Add(stronghold);
            onStrongholdSpawned?.Invoke(stronghold);
        }

        public void OnStrongholdDespawned(Stronghold stronghold)
        {
            if (!_activeStrongholds.Contains(stronghold))
                return;

            _activeStrongholds.Remove(stronghold);
            onStrongholdDespawned?.Invoke(stronghold);
        }

        public Stronghold GetStronghold(FStaticPropPosition strongholdData)
        {
            foreach(var stronghold in  _activeStrongholds) 
            {
                if(stronghold.Data.IsEqual(strongholdData))
                    return stronghold;
            }

            return null;
        }

        public BuildableZone GetBuildableZone(int strongholdId)
        {
            foreach (var stronghold in _activeStrongholds)
            {
                if (stronghold.StrongholdID == strongholdId)
                    return stronghold.BuildableZone;
            }

            return null;
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_ActivateNexus(FStaticPropPosition staticPropPosition)
        {
            var position = staticPropPosition.GetPosition(Context, HasStateAuthority);

            // Create particle here
            StandaloneVisualEffect visualEffect = DWDObjectPool.Instance.SpawnAt(_preSpawnVisualEffect, position) as StandaloneVisualEffect;
            visualEffect.Initialize();

            var containerData = Context.ContainerManager.GetContainerFreeReplicatorAndIndex(BuildableConstants.MAX_WORKERS_PER_STRONGHOLD);

            if (containerData.freeIndex < 0)
            {
                Debug.Log("No Free Container Index");
                return;
            }

            int containerIndex = (ushort)(containerData.freeIndex + (containerData.replicator.Index * ContainerConstants.CONTAINERS_PER_REPLICATOR));
            Context.ContainerManager.SetupContainer(BuildableConstants.MAX_WORKERS_PER_STRONGHOLD);

            if (HasStateAuthority)
            {
                SpawnStronghold(_activeStrongholds.Count, staticPropPosition, 1000, 1, containerIndex);
            }
        }

        public Stronghold SpawnStronghold(int strongholdId, FStaticPropPosition strongholdData, int health, int rank, int containerIndex)
        {
            var position = GetStrongholdPosition(strongholdData);
            return Runner.Spawn(_strongholdPrefab, position, Quaternion.identity, null,
                                onBeforeSpawned: (runner, obj) =>
                                {
                                    var r = obj.GetComponent<Stronghold>();
                                    r.SetSpawnData(strongholdId, strongholdData, health, rank, containerIndex);
                                });
            
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

        public PropRuntimeState GetNexusState(FStaticPropPosition nexusData)
        {
            Chunk chunk = Context.ChunkManager.GetChunk(nexusData.ChunkPosition);
            if (chunk != null && chunk.GetRenderState(HasStateAuthority, nexusData.PropIndex, out var state))
            {
                return state;
            }
        
            return null;
        }

        public Stronghold GetStronghold(int strongholdId)
        {
            foreach (var stronghold in _activeStrongholds)
            { 
                if(stronghold.StrongholdID == strongholdId)
                    return stronghold;
            }

            return null;
        }

        public Vector3 GetStrongholdPosition(FStaticPropPosition strongholdData)
        {


            return Vector3.zero;
        }
    }
}
