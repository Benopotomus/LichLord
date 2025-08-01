using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.Props
{
    [CreateAssetMenu(fileName = "PropDefinition", menuName = "LichLord/Props/PropDefinition")]
    public class PropDefinition : TableObject
    {
        [SerializeField]
        protected string _name;
        public string Name => _name;

        //Visuals
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _prefabBundle;
        public BundleObject PrefabBundle => _prefabBundle;

        //Networking
        [SerializeField]
        protected PropDataDefinition _propDataDefinition;
        public PropDataDefinition PropDataDefinition => _propDataDefinition;

#if UNITY_EDITOR
        public GameObject prefab;
#endif
    }
}