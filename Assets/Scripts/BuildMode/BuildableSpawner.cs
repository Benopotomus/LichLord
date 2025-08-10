using DWD.Pooling;
using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableSpawner
    {
        public Action<int, Buildable> OnBuildableSpawned;

        public void SpawnBuildable(BuildableZone zone, 
            int index,
            BuildableDefinition definition, 
            Vector3 spawnPosition, 
            Quaternion spawnRotation, 
            int stateData)
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
                    OnPrefabLoaded(zone, index, definition, spawnPosition, spawnRotation, stateData, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            BuildableLoader buildableLoader = new BuildableLoader(zone, 
                index,
                definition, 
                spawnPosition, 
                spawnRotation, 
                stateData, 
                prefabLoader);


            if (buildableLoader.Loader != null)
            {
                if (buildableLoader.Loader.IsLoaded)
                    OnLoaderLoaded(buildableLoader);
                else
                    buildableLoader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        private void OnLoaderLoaded(BuildableLoader buildableLoader)
        {
            buildableLoader.OnLoadComplete -= OnLoaderLoaded;

            OnPrefabLoaded(buildableLoader.Zone, 
                buildableLoader.Index,
                buildableLoader.Definition, 
                buildableLoader.Position, 
                buildableLoader.Rotation,
                buildableLoader.Data,
                buildableLoader.Loader);
        }

        private void OnPrefabLoaded(BuildableZone zone, 
            int index,
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

            OnBuildableSpawned?.Invoke(index, spawnedBuildable);
        }
    }
}