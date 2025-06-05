using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "NonPlayerCharacterDefinition", menuName = "LichLord/NonPlayerCharacters/NonPlayerCharacterDefinition")]
    public class NonPlayerCharacterDefinition : TableObject
    {
        [SerializeField]
        protected string _name;
        public string Name => _name;

        //Visuals
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _prefabBundle;
        public BundleObject PrefabBundle => _prefabBundle;

        //Visuals
        [SerializeField]
        protected NonPlayerCharacterUpdateDefinition _updateDefinition;
        public NonPlayerCharacterUpdateDefinition UpdateDefinition => _updateDefinition;
    }
}