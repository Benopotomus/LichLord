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
    public class LairManager : ContextBehaviour
    {
        private List<PropRuntimeState> _authorityNexusStates = new List<PropRuntimeState>();
        private List<PropRuntimeState> _predictedStates = new List<PropRuntimeState>();

        public Action<Lair> onLairSpawned;
        public Action<Lair> onLairDespawned;

        [SerializeField] private Lair _lairPrefab;
        [SerializeField] private StandaloneVisualEffect _preSpawnVisualEffect;

        [SerializeField]
        private List<Lair> _activeLairs = new List<Lair>();
        public List<Lair> ActiveLairs => _activeLairs;

        public void LoadLairs()
        {
            if (!HasStateAuthority)
                return;

            var loadedLairs = Context.WorldSaveLoadManager.LoadedLairs;

            foreach (FStrongholdSaveData strongholdSaveData in loadedLairs)
            {
                FStaticPropPosition propPosition  = new FStaticPropPosition();
                propPosition.ChunkPosition = strongholdSaveData.chunkCoord;
                propPosition.PropIndex = (ushort)strongholdSaveData.index;

                Lair strongholdSpawned = SpawnStronghold(strongholdSaveData.strongholdId, 
                    propPosition, 
                    strongholdSaveData.currentHealth, 
                    strongholdSaveData.rank,
                    strongholdSaveData.containerIndex);

                strongholdSpawned.BuildableZone.LoadBuildables(strongholdSaveData.buildableStates);
                strongholdSpawned.WorkerComponent.LoadWorkerData(strongholdSaveData.workerSaveDatas);
            }
        }

        public void OnLairSpawned(Lair lair)
        {
            if (_activeLairs.Contains(lair))
                return;

            _activeLairs.Add(lair);
            onLairSpawned?.Invoke(lair);
        }

        public void OnLairDespawned(Lair lair)
        {
            if (!_activeLairs.Contains(lair))
                return;

            _activeLairs.Remove(lair);
            onLairDespawned?.Invoke(lair);
        }

        public Lair GetLair(FStaticPropPosition lairData)
        {
            foreach(var lair in  _activeLairs) 
            {
                if(lair.Data.IsEqual(lairData))
                    return lair;
            }

            return null;
        }

        public Lair GetLair(int lairId)
        {
            foreach (var lair in _activeLairs)
            {
                if (lair.LairID == lairId)
                    return lair;
            }

            return null;
        }

        public BuildableZone GetBuildableZone(int lairId)
        {
            foreach (var stronghold in _activeLairs)
            {
                if (stronghold.LairID == lairId)
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
                SpawnStronghold(_activeLairs.Count, staticPropPosition, 1000, 1, containerIndex);
            }
        }

        public Lair SpawnStronghold(int strongholdId, FStaticPropPosition strongholdData, int health, int rank, int containerIndex)
        {
            var position = GetStrongholdPosition(strongholdData);
            return Runner.Spawn(_lairPrefab, position, Quaternion.identity, null,
                                onBeforeSpawned: (runner, obj) =>
                                {
                                    var r = obj.GetComponent<Lair>();
                                    r.SetSpawnData(strongholdId, strongholdData, health, rank, containerIndex);
                                });
            
        }

        // Get the nearest runtime state for a nexus
        public Lair GetNearestStronghold(Vector3 playerPosition)
        {
            Lair nearestStronghold = null; // Reset to null each frame
            float minSqrDistance = float.MaxValue;

            foreach (var stronghold in _activeLairs)
            {
                float sqrDist = Vector3.SqrMagnitude(stronghold.Position - playerPosition);
                // Debug.Log($"Nexus {nexusData.GUID} Health: {state.GetHealth()}"); // Uncomment for debugging

                if (sqrDist < minSqrDistance)
                {
                    minSqrDistance = sqrDist;
                    nearestStronghold = stronghold;
                }
                
            }

            return nearestStronghold;
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

        public Lair GetStronghold(int strongholdId)
        {
            foreach (var stronghold in _activeLairs)
            { 
                if(stronghold.LairID == strongholdId)
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
