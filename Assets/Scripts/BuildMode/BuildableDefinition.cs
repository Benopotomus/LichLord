using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableDefinition : TableObject
    {
        [SerializeField]
        protected string _name;
        public string Name => _name;

        //Visuals
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _prefabBundle;
        public BundleObject PrefabBundle => _prefabBundle;
    }
}
