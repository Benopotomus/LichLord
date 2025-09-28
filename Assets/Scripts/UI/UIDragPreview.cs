using DWD.Utility.Loading;
using LichLord.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIDragPreview : MonoBehaviour
    {
        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private TextMeshProUGUI _countText;

        [SerializeField]
        private FItemData _itemData;

        [SerializeField]
        private ItemDefinition _itemDefinition;

        [SerializeField]
        private RectTransform _rectTransform;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        private IconLoader _iconLoader = new IconLoader();

        private void Awake()
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _canvasGroup.alpha = 0.8f; // Semi-transparent
        }

        public void SetItemData(FItemData itemData)
        {
            _itemData.Copy(in itemData);
            _itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID);

            if (_itemDefinition != null)
            {
                LoadIcon(_itemDefinition.Icon);
                int stackCount = _itemDefinition.DataDefinition.GetStackCount(ref _itemData);
                _countText.text = stackCount.ToString();
                _countText.enabled = stackCount > 1; // Hide count for single items
            }
            else
            {
                _iconImage.enabled = false;
                _countText.enabled = false;
            }
        }

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

        public void UpdatePosition(Vector2 localPoint)
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = localPoint;
            }
        }
    }
}