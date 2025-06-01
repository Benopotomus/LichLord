using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.Props
{
    [System.Serializable]
    public class PropLoader
    {
        private PropRuntimeState _runtimeState;
        public PropRuntimeState RuntimeState => _runtimeState;

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
        public System.Action<PropLoader> OnLoadComplete;

        public PropLoader() { }
        public PropLoader(PropRuntimeState runtimeState,
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
