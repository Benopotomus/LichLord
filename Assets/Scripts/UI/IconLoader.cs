using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.UI
{
    public class IconLoader
    {
        private AssetBundleLoader _iconLoader;

        public Action<IconLoader, Sprite> OnLoaded;

        public void LoadIcon(BundleObject prefabBundle)
        {
            if (prefabBundle.Ready == false)
            {
                Debug.LogWarning("Cannot spawn Icon for " + prefabBundle.Name + ". Bundle is not ready.");
                return;
            }

            _iconLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            if (_iconLoader != null)
            {
                if (_iconLoader.IsLoaded)
                    SpawnLoadedIcon(_iconLoader);
                else
                    _iconLoader.OnLoadComplete += OnVisualsPrefabLoaded;
            }
        }

        private void OnVisualsPrefabLoaded(ILoader clipLoader)
        {
            if (_iconLoader != null)
                _iconLoader.OnLoadComplete -= OnVisualsPrefabLoaded;

            SpawnLoadedIcon(clipLoader);
        }

        private void SpawnLoadedIcon(ILoader clipLoader)
        {
            AssetBundleLoader loader = clipLoader as AssetBundleLoader;
            Sprite sprite = loader.GetAssetWithin<Sprite>();

            OnLoaded?.Invoke(this, sprite);
        }
    }
}
