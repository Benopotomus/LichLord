using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    public class ItemSpawner
    {
        public Action<GameObject, Transform, Quaternion> OnLoadedAttached;
        public Action<GameObject, Vector3, Quaternion> OnLoaded;

        public void SpawnItemAttached(Transform attachment, Quaternion rotation, BundleObject prefabBundle)
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
                    OnPrefabLoaded(attachment, attachment.position, rotation, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            ItemLoader impactLoader = new ItemLoader(attachment, rotation, prefabLoader);

            if (impactLoader.Loader != null)
            {
                if (impactLoader.Loader.IsLoaded)
                    OnLoaderLoaded(impactLoader);
                else
                    impactLoader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        public void SpawnItem(Vector3 position, Quaternion rotation, BundleObject prefabBundle)
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
                    OnPrefabLoaded(null, position, rotation, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            ItemLoader impactLoader = new ItemLoader(position, rotation, prefabLoader);

            if (impactLoader.Loader != null)
            {
                if (impactLoader.Loader.IsLoaded)
                    OnLoaderLoaded(impactLoader);
                else
                    impactLoader.OnLoadComplete += OnLoaderLoaded;
            }
        }

        private void OnLoaderLoaded(ItemLoader impactLoader)
        {
            impactLoader.OnLoadComplete -= OnLoaderLoaded;

            OnPrefabLoaded(impactLoader.Attachment, impactLoader.Position, impactLoader.Rotation, impactLoader.Loader);
        }

        private void OnPrefabLoaded(Transform attachment, Vector3 position, Quaternion rotation, AssetBundleLoader loadedBundle)
        {
            GameObject go = loadedBundle.GetAssetWithin<GameObject>();

            OnLoadedAttached?.Invoke(go, attachment, rotation);
        }
    }
}