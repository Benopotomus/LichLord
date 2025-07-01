using DWD.Utility.Loading;
using LichLord.Projectiles;
using UnityEngine;

namespace LichLord.Projectiles
{
    [System.Serializable]
    public class ProjectileVisualLoader
    {
        private FProjectileData _data;
        public FProjectileData Data => _data;

        private AssetBundleLoader _loader;
        public AssetBundleLoader Loader
        {
            get { return _loader; }
            set
            {
                _loader = value;
                _loader.OnLoadComplete += HandleLoaderComplete;
            }
        }

        private GameObject _loadedPrefab;
        public GameObject LoadedPrefab { get { return _loadedPrefab; } }
        public System.Action<ProjectileVisualLoader> OnLoadComplete;

        public ProjectileVisualLoader() { }
        public ProjectileVisualLoader(FProjectileData data,
            AssetBundleLoader iLoader)
        {
            _data.Copy(data);
            Loader = iLoader;
        }

        private void HandleLoaderComplete(ILoader loader)
        {
            _loader.OnLoadComplete -= HandleLoaderComplete;
            _loadedPrefab = Loader.GetAsset<GameObject>();
            if (OnLoadComplete != null)
                OnLoadComplete.Invoke(this);
        }
    }
}
