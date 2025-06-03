using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.Buildables
{
    [System.Serializable]
    public class BuildableLoader
    {
        private BuildableRuntimeState _runtimeState;
        public BuildableRuntimeState RuntimeState => _runtimeState;

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
        public System.Action<BuildableLoader> OnLoadComplete;

        public BuildableLoader() { }
        public BuildableLoader(BuildableRuntimeState runtimeState,
            AssetBundleLoader iLoader)
        {
            _runtimeState = runtimeState;
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
