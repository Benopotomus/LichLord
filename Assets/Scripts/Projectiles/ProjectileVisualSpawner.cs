using DWD.Pooling;
using DWD.Utility.Loading;
using LichLord.Projectiles;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Props
{
    public class ProjectileVisualSpawner 
    {
        public Action<GameObject, FProjectileData> OnProjectileVisualSpawned;

        public void SpawnProjectileVisual(ProjectileDefinition definition, ref FProjectileData data)
        {
            BundleObject prefabBundle = definition.VisualsPrefab;

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
                    OnPrefabLoaded(data, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            ProjectileVisualLoader projectileLoader = new ProjectileVisualLoader(data, prefabLoader);

            if (projectileLoader.Loader != null)
            {
                if (projectileLoader.Loader.IsLoaded)
                    OnLoaderLoaded(projectileLoader);
                else
                    projectileLoader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        private void OnLoaderLoaded(ProjectileVisualLoader propLoader)
        {
            propLoader.OnLoadComplete -= OnLoaderLoaded;

            OnPrefabLoaded(propLoader.Data, propLoader.Loader);
        }

        private void OnPrefabLoaded(FProjectileData data, AssetBundleLoader loadedBundle)
        {
            GameObject go = loadedBundle.GetAssetWithin<GameObject>();
            OnProjectileVisualSpawned?.Invoke(go, data);
        }
    }
}