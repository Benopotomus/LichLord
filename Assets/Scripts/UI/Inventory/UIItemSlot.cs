using DWD.Utility.Loading;
using LichLord.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIItemSlot : UIWidget
    {
        [SerializeField]
        private UIButton _button;

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

            if (_itemDefinition != null)
            {
                LoadIcon(_itemDefinition.Icon);

                int stackCount = _itemDefinition.DataDefintion.GetStackCount(ref _itemData);

                _countText.text = stackCount.ToString();
                _countText.enabled = true;
            }
            else
            {
                _iconImage.enabled = false;
                _countText.enabled = false;
            }
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
            _iconImage.enabled = true;
        }
    }
}
