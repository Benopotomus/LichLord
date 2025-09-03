using DWD.Utility.Loading;
using UnityEngine;
using AYellowpaper.SerializedCollections;

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
        protected int _damageReduction = 3;
        public int DamageReduction => _damageReduction;

        [SerializeField]
        protected float _damageResistance = 0.0f;
        public float DamageResistance => _damageResistance;

        [SerializeField]
        protected float _walkSpeed;
        public float WalkSpeed => _walkSpeed;

        [SerializeField]
        [SerializedDictionary("SpawnType", "DataDefinition")]
        private SerializedDictionary<ENPCSpawnType, NonPlayerCharacterDataDefinition> _spawnTypeDataDefinitions;

        public NonPlayerCharacterDataDefinition GetDataDefinition(ENPCSpawnType spawnType)
        {
            if (_spawnTypeDataDefinitions.TryGetValue(spawnType, out var value))
                return value;

            return null;
        }

        [SerializeField]
        [SerializedDictionary("CurrencyType", "CarryValue")]
        private SerializedDictionary<ECurrencyType, int> _currencyCarryValues;

        public int GetCarryValue(ECurrencyType currencyType)
        { 
            if(_currencyCarryValues.TryGetValue(currencyType, out var value)) 
                return value;

            return 0;
        }

    }
}