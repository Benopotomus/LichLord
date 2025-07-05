using Fusion;
using LichLord.Props;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public partial class BuildableManager : ContextBehaviour
    {
        [SerializeField] private BuildableZone _zonePrefab;
        [SerializeField] private BuildableSaveLoadManager saveLoadManager;

        public override void Spawned()
        {

        }

        public void PlaceBuilding(Vector3 position, int definitionId)
        {
            Debug.Log("PlaceBuilding: " + definitionId);
        }

        private void OnBuildableSpawned(BuildableRuntimeState propRuntimeState, Buildable buildable)
        {

        }

        private void DespawnProp(int guid)
        {
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
        }

        public void SpawnBuildableZone(Vector3 position, Quaternion rotation)
        {
            Runner.Spawn(_zonePrefab, position, rotation);
        }
    }
}