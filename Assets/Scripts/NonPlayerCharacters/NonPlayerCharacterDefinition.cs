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

        [SerializeField]
        protected int _maxHealth;
        public int MaxHealth => _maxHealth;

        [SerializeField]
        protected float _walkSpeed;
        public float WalkSpeed => _walkSpeed;

        [SerializeField] // added radius for NPC targeting/melee
        protected float _bonusRadius;
        public float BonusRadius => _bonusRadius;

    }
}