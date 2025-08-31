using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(menuName = "LichLord/Currency/CurrencyTable")]
    public class CurrencyTable : ScriptableObject
    {
        [SerializeField]
        [SerializedDictionary("SessionID", "WorldSavedData")]
        private SerializedDictionary<ECurrencyType, CurrencyDefinition> _currencyDefinitions;

        public CurrencyDefinition GetDefinition(ECurrencyType currencyType)
        {
            return _currencyDefinitions[currencyType];
        }

    }
}