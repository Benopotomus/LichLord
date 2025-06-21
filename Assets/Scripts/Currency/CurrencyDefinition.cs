using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "Currency", menuName = "LichLord/Currency/CurrencyDefinition", order = 1)]
    public class CurrencyDefinition : TableObject
    {
        public string CurrencyName;

        //UI
        [BundleObject(typeof(Sprite))]
        [SerializeField]
        protected BundleObject _icon;
        public BundleObject Icon => _icon;
    }
}
