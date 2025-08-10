using DWD.Pooling;
using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildablePreviewLoader
    {
        public Action<GameObject> OnBuildablePreviewLoaded;

        private AssetBundleLoader _loader;

        public void SpawnBuildablePreview(BuildableDefinition definition)
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
                    OnPrefabLoaded(loadedBundle);
                    return;
                }
            }

            _loader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;

            if (_loader != null)
            {
                if (_loader.IsLoaded)
                    OnLoaderLoaded(_loader);
                else
                    _loader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        private void OnLoaderLoaded(ILoader clipLoader)
        {
            _loader.OnLoadComplete -= OnLoaderLoaded;
            OnPrefabLoaded(_loader);
        }

        private void OnPrefabLoaded(AssetBundleLoader loadedBundle)
        {
            GameObject prefab = loadedBundle.GetAssetWithin<GameObject>();

            if (prefab == null)
                return;

            OnBuildablePreviewLoaded?.Invoke(prefab);
        }
    }
}