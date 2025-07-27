using DWD.Pooling;
using DWD.Utility.Loading;
using LichLord.Projectiles;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class ImpactSpawner
    {
        public Action<GameObject, Transform, Quaternion> OnImpactSpawnedAttached;
        public Action<GameObject, Vector3, Quaternion> OnImpactSpawned;

        public void SpawnImpactVisualAttached(Transform attachment, Quaternion rotation, BundleObject prefabBundle)
        {
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
                    OnPrefabLoaded(attachment, rotation, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            ImpactVisualLoader impactLoader = new ImpactVisualLoader(attachment, rotation, prefabLoader);

            if (impactLoader.Loader != null)
            {
                if (impactLoader.Loader.IsLoaded)
                    OnLoaderLoaded(impactLoader);
                else
                    impactLoader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        private void OnLoaderLoaded(ImpactVisualLoader impactLoader)
        {
            impactLoader.OnLoadComplete -= OnLoaderLoaded;

            OnPrefabLoaded(impactLoader.Attachment, impactLoader.Rotation, impactLoader.Loader);
        }

        private void OnPrefabLoaded(Transform attachment, Quaternion rotation, AssetBundleLoader loadedBundle)
        {
            GameObject go = loadedBundle.GetAssetWithin<GameObject>();
            OnImpactSpawnedAttached?.Invoke(go, attachment, rotation);
        }
    }
}