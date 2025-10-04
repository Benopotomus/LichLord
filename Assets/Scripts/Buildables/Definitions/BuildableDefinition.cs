using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.Buildables
{

    public class BuildableDefinition : TableObject
    {
        [SerializeField]
        protected string _buildableName;
        public string BuildableName => _buildableName;

        //Visuals
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _prefabBundle;
        public BundleObject PrefabBundle => _prefabBundle;

        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _placementVFX;
        public BundleObject PlacementVFX => _placementVFX;

        //UI
        [BundleObject(typeof(Sprite))]
        [SerializeField]
        protected BundleObject _icon;
        public BundleObject Icon => _icon;

        //Networking
        [SerializeField]
        protected BuildableDataDefinition _buildableDataDefinition;
        public BuildableDataDefinition BuildableDataDefinition => _buildableDataDefinition;

        // Health
        [Header("Health")]
        [SerializeField]
        protected int _maxHealth = 100;
        public int MaxHealth => _maxHealth;

        [SerializeField]
        protected int _damageReduction = 3;
        public int DamageReduction => _damageReduction;

        [SerializeField]
        protected float _damageResistance = 0.0f;
        public float DamageResistance => _damageResistance;

        // Contianer
        [Header("Container")]
        [SerializeField]
        protected int _containerSlots = 8;
        public int ContainerSlots => _containerSlots;

        [SerializeField]
        protected bool _isStockpile;
        public bool IsStockpile => _isStockpile;

    }
}
