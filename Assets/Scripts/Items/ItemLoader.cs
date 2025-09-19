using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord
{
    [System.Serializable]
    public class ItemLoader
    {
        private Transform _attachment; // if null, position is global
        public Transform Attachment => _attachment;
        private Quaternion _rotation;
        public Quaternion Rotation => _rotation;
        private Vector3 _position;
        public Vector3 Position => _position;

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
        public System.Action<ItemLoader> OnLoadComplete;

        public ItemLoader() { }
        public ItemLoader(Transform attachment, Quaternion rotation,
            AssetBundleLoader iLoader)
        {
            _attachment = attachment;
            _rotation = rotation;
            Loader = iLoader;
        }

        public ItemLoader(Vector3 position, Quaternion rotation,
            AssetBundleLoader iLoader)
        {
            _rotation = rotation;
            _position = position;
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
