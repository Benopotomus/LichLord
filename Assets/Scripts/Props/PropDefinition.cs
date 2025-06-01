using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.Props
{
    [CreateAssetMenu(fileName = "PropDefinition", menuName = "LichLord/Props/PropDefinition")]
    public class PropDefinition : TableObject
    {
        [Tooltip("Name of the prop type (e.g., Tree, Rock)")]
        public string propName;

        //Visuals
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _prefabBundle;
        public BundleObject PrefabBundle => _prefabBundle;

        public GameObject prefab;
    }
}