using DWD.Pooling;
using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableFeatureSpawner : MonoBehaviour
    {
        public Action<Buildable, int> OnBuildableFeatureSpawned;

        public void SpawnBuildableFeature(BuildableZone zone,
            int subTileIndex,
            BuildableDefinition definition,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            int data)
        {
            if (definition == null)
            {
                Debug.LogWarning("Trying to spawn prop with invalid definition, id: " + definition.TableID);
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
                    OnPrefabLoaded(zone, subTileIndex, definition, spawnPosition, spawnRotation, data, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            BuildableFeatureLoader buildableLoader = new BuildableFeatureLoader(zone,
                subTileIndex,
                definition,
                spawnPosition,
                spawnRotation,
                data,
                prefabLoader);


            if (buildableLoader.Loader != null)
            {
                if (buildableLoader.Loader.IsLoaded)
                    OnLoaderLoaded(buildableLoader);
                else
                    buildableLoader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        private void OnLoaderLoaded(BuildableFeatureLoader buildableFeatureLoader)
        {
            buildableFeatureLoader.OnLoadComplete -= OnLoaderLoaded;

            OnPrefabLoaded(buildableFeatureLoader.Zone,
                buildableFeatureLoader.SubTileIndex,
                buildableFeatureLoader.Definition,
                buildableFeatureLoader.Position,
                buildableFeatureLoader.Rotation,
                buildableFeatureLoader.Data,
                buildableFeatureLoader.Loader);
        }

        private void OnPrefabLoaded(BuildableZone zone,
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

            OnBuildableFeatureSpawned?.Invoke(spawnedBuildable, floorTileIndex);
        }
    }
}