using DWD.Utility.Loading;
using LichLord.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIItemSlot : UIButton
    {
        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private TextMeshProUGUI _countText;

        [SerializeField]
        private FItem _itemData;

        [SerializeField]
        private ItemDefinition _itemDefinition;

        protected IconLoader _iconLoader = new IconLoader();

        public void SetItemData(FItem itemData)
        { 
            _itemData.Copy(in itemData);

            _itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID);

            LoadIcon(_itemDefinition.Icon);
        }

        // VISUALS

        private void LoadIcon(BundleObject prefabBundle)
        {
            _iconLoader.OnLoaded += OnIconLoaded;
            _iconLoader.LoadIcon(prefabBundle);
        }

        private void OnIconLoaded(IconLoader iconLoader, Sprite sprite)
        {
            _iconLoader.OnLoaded -= OnIconLoaded;
            _iconImage.sprite = sprite;
        }
    }
}
