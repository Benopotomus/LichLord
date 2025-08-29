using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "Currency", menuName = "LichLord/Currency/CurrencyDefinition", order = 1)]
    public class CurrencyDefinition : TableObject
    {
        public string CurrencyName;

        [SerializeField]
        private ECurrencyType _currencyType;
        public ECurrencyType CurrencyType => _currencyType;

        //UI
        [BundleObject(typeof(Sprite))]
        [SerializeField]
        protected BundleObject _icon;
        public BundleObject Icon => _icon;
    }

    public enum ECurrencyType : byte // 16
    { 
        None,
        Wood,
        Stone,
        Iron,

        Souls,
        Gold,
        Deathcaps,
        Bones,

        Diamonds,
        Oil,
        Linen,
        Parchment,

        Obsidian,
        Ectoplasm,
        Relics,
        Ichor,
    }
}
