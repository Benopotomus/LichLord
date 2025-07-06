using DWD.Pooling;
using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableWallSpawner : MonoBehaviour
    {
        public Action<Buildable, int, EWallOrientation> OnBuildableWallSpawned;

        public void SpawnBuildableWall(BuildableZoneFloor floor, 
            int floorTileIndex,
            EWallOrientation wallOrientation,
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
                    OnPrefabLoaded(floor, floorTileIndex, wallOrientation, definition, spawnPosition, spawnRotation, data, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            BuildableWallLoader buildableWallLoader = new BuildableWallLoader(floor,
                floorTileIndex,
                wallOrientation,
                definition, 
                spawnPosition, 
                spawnRotation, 
                data, 
                prefabLoader);


            if (buildableWallLoader.Loader != null)
            {
                if (buildableWallLoader.Loader.IsLoaded)
                    OnLoaderLoaded(buildableWallLoader);
                else
                    buildableWallLoader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        private void OnLoaderLoaded(BuildableWallLoader buildableWallLoader)
        {
            buildableWallLoader.OnLoadComplete -= OnLoaderLoaded;

            OnPrefabLoaded(buildableWallLoader.Floor,
                buildableWallLoader.FloorTileIndex,
                buildableWallLoader.Orientation,
                buildableWallLoader.Definition,
                buildableWallLoader.Position,
                buildableWallLoader.Rotation,
                buildableWallLoader.Data,
                buildableWallLoader.Loader);
        }

        private void OnPrefabLoaded(BuildableZoneFloor floor,
            int floorTileIndex,
            EWallOrientation wallOrientation,
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

            OnBuildableWallSpawned?.Invoke(spawnedBuildable, floorTileIndex, wallOrientation);
        }
    }
}