using DWD.Pooling;
using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableSpawner : MonoBehaviour
    {
        public Action<BuildableRuntimeState, Buildable> OnBuildableSpawned;

        public void SpawnProp(BuildableRuntimeState buildRuntimeState)
        {
            BuildableDefinition definition = Global.Tables.BuildableTable.TryGetDefinition(buildRuntimeState.definitionId);

            if (definition == null)
            {
                Debug.LogWarning("Trying to spawn prop with invalid definition, id: " + buildRuntimeState.definitionId);
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
                    OnPrefabLoaded(buildRuntimeState, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            BuildableLoader propLoader = new BuildableLoader(buildRuntimeState, prefabLoader);

            if (propLoader.Loader != null)
            {
                if (propLoader.Loader.IsLoaded)
                    OnPrefabLoaded(propLoader);
                else
                    propLoader.OnLoadComplete += OnPrefabLoaded;
            }
        }

        private void OnPrefabLoaded(BuildableLoader propLoader)
        {
            propLoader.OnLoadComplete -= OnPrefabLoaded;

            OnPrefabLoaded(propLoader.RuntimeState, propLoader.Loader);
        }

        private void OnPrefabLoaded(BuildableRuntimeState runtimeState, AssetBundleLoader loadedBundle)
       {
            GameObject prefab = loadedBundle.GetAssetWithin<GameObject>();

            if (prefab == null)
                return;

            var poolObject = prefab.GetComponent<DWDObjectPoolObject>();
            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn prop " + runtimeState.definitionId + ".  Could not find DWDObjectPoolObject Component!");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAt(poolObject, runtimeState.position, runtimeState.rotation);

            Buildable spawnedBuildable = instance.GetComponent<Buildable>();

            if (spawnedBuildable == null)
            {
                Debug.LogWarning("Prop is Invalid, Check Bundles! (" + loadedBundle.BundleName + ")");
                return;
            }

            OnBuildableSpawned?.Invoke(runtimeState, spawnedBuildable);
        }
    }
}