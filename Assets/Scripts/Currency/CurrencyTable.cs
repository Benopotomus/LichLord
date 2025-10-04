using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(menuName = "LichLord/Currency/CurrencyTable")]
    public class CurrencyTable : ScriptableObject
    {
        [SerializeField]
        [SerializedDictionary("CurrencyType", "CurrencyDefinition")]
        private SerializedDictionary<ECurrencyType, CurrencyDefinition> _currencyDefinitions;

        public CurrencyDefinition TryGetDefinition(ECurrencyType currencyType)
        {
            return _currencyDefinitions[currencyType];
        }

    }
}