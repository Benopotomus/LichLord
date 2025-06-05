using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [System.Serializable]
    public class NonPlayerCharacterLoader
    {
        private NonPlayerCharacterRuntimeState _runtimeState;
        public NonPlayerCharacterRuntimeState RuntimeState => _runtimeState;

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
        public System.Action<NonPlayerCharacterLoader> OnLoadComplete;

        public NonPlayerCharacterLoader() { }
        public NonPlayerCharacterLoader(NonPlayerCharacterRuntimeState runtimeState,
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
