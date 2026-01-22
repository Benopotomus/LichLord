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
        protected Image _iconImage;
        public Image IconImage => _iconImage;

        [SerializeField]
        protected TextMeshProUGUI _countText;

        [SerializeField]
        protected FItemData _itemData;
        public ref FItemData ItemData => ref _itemData;

        [SerializeField]
        protected ItemDefinition _itemDefinition;

        private IconLoader _iconLoader = new IconLoader();

        public virtual void SetItemData(FItemData itemData)
        {
            if (_itemData.IsEqual(itemData))
                return;

            _itemData.Copy(in itemData);
            _itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID);

            if (_itemDefinition != null)
            {
                LoadIcon(_itemDefinition.Icon);
                int stackCount = _itemDefinition.DataDefinition.GetStackCount(ref _itemData);
                if (_countText != null)
                { 
                    _countText.text = stackCount.ToString();
                    _countText.enabled = stackCount > 1; // Hide count for single items
                }
            }
            else
            {
                _iconImage.enabled = false;
                _countText.enabled = false;
            }
        }

        protected void LoadIcon(BundleObject prefabBundle)
        {
            _iconLoader.OnLoaded += OnIconLoaded;
            _iconLoader.LoadIcon(prefabBundle);
        }

        protected void OnIconLoaded(IconLoader iconLoader, Sprite sprite)
        {
            _iconLoader.OnLoaded -= OnIconLoaded;
            _iconImage.sprite = sprite;
            _iconImage.enabled = true;
        }
    }
}