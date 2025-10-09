using DWD.Utility.Loading;
using LichLord.Items;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "Currency", menuName = "LichLord/Currency/CurrencyDefinition", order = 1)]
    public class CurrencyDefinition : ItemDefinition
    {
        public string CurrencyName;

        [SerializeField]
        private ECurrencyType _currencyType;
        public ECurrencyType CurrencyType => _currencyType;

    }

    public enum ECurrencyType : byte // 32
    { 
        None,
        Wood,
        Stone,
        IronOre,

        Souls,
        Gold,
        Deathcaps,
        Bones,

        IronBar,
        Charcoal,
        Linen,
        Parchment,

        Obsidian,
        Ectoplasm,
        Fiber,
        Ichor,
    }
}
