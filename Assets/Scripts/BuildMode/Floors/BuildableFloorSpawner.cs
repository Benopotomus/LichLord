using DWD.Pooling;
using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableFloorSpawner : MonoBehaviour
    {
        public Action<Buildable, int> OnBuildableFloorSpawned;

        public void SpawnBuildableFloor(BuildableZoneReplicator floor, 
            int floorTileIndex,
            BuildableDefinition definition, 
            Vector3 spawnPosition, 
            Quaternion spawnRotation, 
            int data)
        {
            if (definition == null)
            {
                Debug.LogWarning("Trying to spawn prop with invalid definition, id: " + definition);
                return;
            }

            BundleObject prefabBundle = definition.PrefabBundle;

            if (!prefabBundle.Ready)
            {
                Debug.LogWarning("Cannot load null Bundle Object! ");
                return;
            }

            List<ILoader> LoadedBundles = AssetBundleManager.Instance.CompleteLoaders;

            for (int i = 0; i < LoadedBundles.Count; i++)
            {
                AssetBundleLoader loadedBundle = LoadedBundles[i] as AssetBundleLoader;

                if (loadedBundle.BundleName == prefabBundle.Bundle)
                {
                    OnPrefabLoaded(floor, floorTileIndex, definition, spawnPosition, spawnRotation, data, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            BuildableFloorLoader buildableFloorLoader = new BuildableFloorLoader(floor,
                floorTileIndex,
                definition, 
                spawnPosition, 
                spawnRotation, 
                data, 
                prefabLoader);


            if (buildableFloorLoader.Loader != null)
            {
                if (buildableFloorLoader.Loader.IsLoaded)
                    OnLoaderLoaded(buildableFloorLoader);
                else
                    buildableFloorLoader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        private void OnLoaderLoaded(BuildableFloorLoader buildableFloorLoader)
        {
            buildableFloorLoader.OnLoadComplete -= OnLoaderLoaded;

            OnPrefabLoaded(buildableFloorLoader.Floor,
                buildableFloorLoader.FloorTileIndex,
                buildableFloorLoader.Definition,
                buildableFloorLoader.Position,
                buildableFloorLoader.Rotation,
                buildableFloorLoader.Data,
                buildableFloorLoader.Loader);
        }

        private void OnPrefabLoaded(BuildableZoneReplicator floor,
            int floorTileIndex,
            BuildableDefinition definition,
            Vector3 position,
            Quaternion rotation,
            int data,
            AssetBundleLoader loadedBundle)
       {
            GameObject prefab = loadedBundle.GetAssetWithin<GameObject>();

            if (prefab == null)
                return;

            var poolObject = prefab.GetComponent<DWDObjectPoolObject>();
            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn prop " + definition + ".  Could not find DWDObjectPoolObject Component!");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAt(poolObject, position, rotation);

            Buildable spawnedBuildable = instance.GetComponent<Buildable>();

            if (spawnedBuildable == null)
            {
                //Debug.LogWarning("Prop is Invalid, Check Bundles! (" + loadedBundle.BundleName + ")");
                return;
            }

            OnBuildableFloorSpawned?.Invoke(spawnedBuildable, floorTileIndex);
        }
    }
}