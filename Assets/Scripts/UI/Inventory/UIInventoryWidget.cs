using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{ 
    public class UIInventoryWidget : UIWidget
    {
        [SerializeField] private UIButton _closeButton;

        [SerializeField] private List<UIItemSlot> _inventorySlots = new List<UIItemSlot>();

        public void Awake()
        {
            _closeButton.onClick.AddListener(OnClosePressed);
        }

        private void OnClosePressed()
        {
            if (Context.UI is GameplayUI gameplayUI)
            { 
                gameplayUI.Close(gameplayUI.InventoryView);
            }
        }

        protected override void OnTick()
        {
            base.OnTick();

            RefreshInventoryItems();
        }

        public void RefreshInventoryItems()
        {
            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            for (int i = 0; i < _inventorySlots.Count; i++)
            {
                var itemData = pc.Inventory.GetItemAtInventorySlot(i);
                _inventorySlots[i].SetItemData(itemData);
            }
            
        }
    }
}
