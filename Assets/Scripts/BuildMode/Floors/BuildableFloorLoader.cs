using DWD.Utility.Loading;
using TMPro;
using UnityEngine;

namespace LichLord.Buildables
{
    [System.Serializable]
    public class BuildableFloorLoader
    {
        private BuildableZoneReplicator _floor;
        public BuildableZoneReplicator Floor => _floor;

        private BuildableDefinition _definition;
        public BuildableDefinition Definition => _definition;

        private int _floorTileIndex;
        public int FloorTileIndex => _floorTileIndex;

        private Vector3 _position;
        public Vector3 Position => _position;

        private Quaternion _rotation;
        public Quaternion Rotation => _rotation;

        private int _data;
        public int Data => _data;

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
        public System.Action<BuildableFloorLoader> OnLoadComplete;

        public BuildableFloorLoader() { }
        public BuildableFloorLoader(BuildableZoneReplicator floor,
            int floorTileIndex,
            BuildableDefinition definition,
            Vector3 position,
            Quaternion rotation,
            int data,
            AssetBundleLoader iLoader)
        {
            _floor = floor;
            _floorTileIndex = floorTileIndex;
            _definition = definition;
            _position = position;
            _rotation = rotation;
            _data = data;
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
