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
            PropDefinition definition = Global.Tables.PropTable.TryGetDefinition(propRuntimeState.definitionID);

            if (definition == null)
            {
                Debug.LogWarning("Trying to spawn prop with invalid definition, id: " + propRuntimeState.definitionID);
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
                    OnPrefabLoaded(propLoader);
                else
                    propLoader.OnLoadComplete += OnPrefabLoaded;
            }
        }

        private void OnPrefabLoaded(PropLoader propLoader)
        {
            propLoader.OnLoadComplete -= OnPrefabLoaded;

            OnPrefabLoaded(propLoader.RuntimeState, propLoader.Loader);
        }

        private void OnPrefabLoaded(PropRuntimeState runtimeState, AssetBundleLoader loadedBundle)
       {
            GameObject prefab = loadedBundle.GetAssetWithin<GameObject>();

            if (prefab == null)
                return;

            GameObject instance = Instantiate(prefab, runtimeState.position, runtimeState.rotation);

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