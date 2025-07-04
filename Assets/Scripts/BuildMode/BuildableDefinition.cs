using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(menuName = "LichLord/Buildables/BuildableDefinition")]
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

        //UI
        [BundleObject(typeof(Sprite))]
        [SerializeField]
        protected BundleObject _icon;
        public BundleObject Icon => _icon;
    }

}
