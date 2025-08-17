using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.Buildables
{

    public class BuildableDefinition : TableObject
    {
        [SerializeField]
        protected string _buildableName;
        public string BuildableName => _buildableName;

        //Visuals
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _prefabBundle;
        public BundleObject PrefabBundle => _prefabBundle;

        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _placementVFX;
        public BundleObject PlacementVFX => _placementVFX;

        //UI
        [BundleObject(typeof(Sprite))]
        [SerializeField]
        protected BundleObject _icon;
        public BundleObject Icon => _icon;

        //Networking
        [SerializeField]
        protected BuildableDataDefinition _buildableDataDefinition;
        public BuildableDataDefinition BuildableDataDefinition => _buildableDataDefinition;

    }
}
