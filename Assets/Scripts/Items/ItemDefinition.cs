namespace LichLord.Items
{
    using DWD.Utility.Loading;
    using UnityEngine;

    public class ItemDefinition : TableObject
    {
        //UI

        [SerializeField]
        private string _displayName;
        public string DisplayName => _displayName;

        [SerializeField]
        protected string _description;
        public string Description => _description;

        [BundleObject(typeof(Sprite))]
        [SerializeField]
        protected BundleObject _icon;
        public BundleObject Icon => _icon;

        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _model;
        public BundleObject Model => _model;

        [SerializeField] private ItemDataDefinition _dataDefintion;
        public ItemDataDefinition DataDefintion => _dataDefintion;

        [SerializeField]
        private int _maxStackCount;
        public virtual int MaxStackCount => _maxStackCount;

        public Color GetColorByQuality(EQuality quality)
        {
            switch (quality)
            {
                case EQuality.None:
                    return new Color(1, 0, 1, 1);
                case EQuality.Common:
                    return new Color(1, 1, 1, 1);
                case EQuality.Uncommon:
                    return new Color(0.12f, 1, 0, 1);
                case EQuality.Rare:
                    return new Color(0, 0.64f, 1f, 1);
                case EQuality.Epic:
                    return new Color(0.64f, 0.21f, 0.93f, 1);
                case EQuality.Legendary:
                    return new Color(1, 0.5f, 0, 1);
                case EQuality.Artifact:
                    return new Color(0.9f, 0.8f, 0.5f, 1);
            }

            return new Color(0, 0, 0, 1);
        }
    }

    public enum EQuality : byte
    {
        None,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Artifact,
    }
}
