using DWD.Utility.Loading;
using LichLord.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIItemSlot : UIWidget, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField]
        private UIButton _button;

        [SerializeField]
        private Image _iconImage;
        public Image IconImage => _iconImage;

        [SerializeField]
        private TextMeshProUGUI _countText;

        [SerializeField]
        protected FItemData _itemData;
        public ref FItemData ItemData => ref _itemData;

        [SerializeField]
        protected ItemDefinition _itemDefinition;

        private IconLoader _iconLoader = new IconLoader();

        // Drag preview
        [SerializeField]
        private UIDragPreview _dragPreviewPrefab;
        private UIDragPreview _dragPreview;

        private void Awake()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_dragPreview != null)
            {
                Destroy(_dragPreview.gameObject);
            }
        }

        public void SetItemData(FItemData itemData)
        {
            if (_itemData.IsEqual(itemData))
                return;

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

        private void OnClick()
        {
            _button.PlayClickSound();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_itemDefinition == null || _iconImage.sprite == null) return;

            CreateDragPreview();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_itemDefinition == null || _dragPreview == null) return;

            if (Context.UI is GameplayUI gameplayUI)
            {
                Vector2 localPoint = ScreenToLocalPosition(
                    eventData.position,
                    gameplayUI.InventoryView.RectTransform,
                    gameplayUI.Canvas
                );

                //Debug.Log($"Screen Position: {eventData.position}, Local Point: {localPoint}, Canvas Size: {gameplayUI.Canvas.GetComponent<RectTransform>().sizeDelta}, HUD Pivot: {gameplayUI.HUD.RectTransform.pivot}");
                _dragPreview.UpdatePosition(localPoint);
            }
        }

        private Vector2 ScreenToLocalPosition(Vector2 screenPosition, RectTransform targetRect, Canvas canvas)
        {
            // Get Canvas RectTransform
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // Use screen size as the effective Canvas size in Overlay mode
            Vector2 canvasSize = new Vector2(Screen.width, Screen.height);
            Vector2 canvasPivot = canvasRect.pivot;

            // Convert screen position to normalized coordinates (0 to 1)
            Vector2 normalizedPos = new Vector2(
                screenPosition.x / canvasSize.x,
                screenPosition.y / canvasSize.y
            );

            // Convert to Canvas local coordinates, accounting for pivot
            Vector2 localPoint = new Vector2(
                (normalizedPos.x - canvasPivot.x) * canvasSize.x,
                (normalizedPos.y - canvasPivot.y) * canvasSize.y
            );

            // Apply CanvasScaler adjustment using the uniform scale factor
            float scaleFactor = canvas.scaleFactor;
            if (scaleFactor != 0f) // Avoid divide by zero
            {
                localPoint /= scaleFactor;
            }

            // Convert from Canvas local space to targetRect's local space
            Vector3 worldPoint = canvasRect.TransformPoint(localPoint);
            Vector2 targetLocalPoint = targetRect.InverseTransformPoint(worldPoint);

            return targetLocalPoint;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            DestroyDragPreview();

            if (_itemDefinition == null) 
                return;

            GameObject droppedOn = eventData.pointerCurrentRaycast.gameObject;
            UIItemSlot targetSlot = droppedOn?.GetComponent<UIItemSlot>();
            Debug.Log(droppedOn);

            if (targetSlot == null || targetSlot == this)
                return;

            Debug.Log(targetSlot);

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            PlayerInventoryComponent inventory = pc.Inventory;

        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handled in OnEndDrag
        }

        private void CreateDragPreview()
        {
            if (Context.UI is GameplayUI gameplayUI)
            {

                var parentTransform = gameplayUI.InventoryView.RectTransform;

                // Instantiate a new UIItemSlot as the drag preview
                _dragPreview = Instantiate(_dragPreviewPrefab, parentTransform, false);

                // _dragPreview.transform.localScale = Vector3.one * 1.1f; // Slight scale

                // Copy item data to preview
                _dragPreview.SetItemData(_itemData);

                // Position at original slot
                _dragPreview.UpdatePosition(_iconImage.rectTransform.position);
                _dragPreview.transform.SetAsLastSibling();
            }
        }

        protected void DestroyDragPreview()
        {
            if (_dragPreview != null)
            {
                Destroy(_dragPreview.gameObject);
                _dragPreview = null;
            }
        }
    }
}