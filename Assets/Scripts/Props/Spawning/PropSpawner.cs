using DWD.Pooling;
using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Props
{
    public class PropSpawner : MonoBehaviour
    {
        public Action<PropRuntimeState, Prop> OnPropSpawned;

        public void SpawnProp(PropRuntimeState propRuntimeState)
        {
            PropDefinition definition = Global.Tables.PropTable.TryGetDefinition(propRuntimeState.definitionId);

            if (definition == null)
            {
                Debug.LogWarning("Trying to spawn prop with invalid definition, id: " + propRuntimeState.definitionId);
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
                    OnPrefabLoaded(propRuntimeState, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            PropLoader propLoader = new PropLoader(propRuntimeState, prefabLoader);

            if (propLoader.Loader != null)
            {
                if (propLoader.Loader.IsLoaded)
                    OnLoaderLoaded(propLoader);
                else
                    propLoader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        private void OnLoaderLoaded(PropLoader propLoader)
        {
            propLoader.OnLoadComplete -= OnLoaderLoaded;

            OnPrefabLoaded(propLoader.RuntimeState, propLoader.Loader);
        }

        private void OnPrefabLoaded(PropRuntimeState runtimeState, AssetBundleLoader loadedBundle)
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
            
            Prop spawnedProp = instance.GetComponent<Prop>();

            if (spawnedProp == null)
            {
                Debug.LogWarning("Prop is Invalid, Check Bundles! (" + loadedBundle.BundleName + ")");
                return;
            }

            OnPropSpawned?.Invoke(runtimeState, spawnedProp);
        }
    }
}