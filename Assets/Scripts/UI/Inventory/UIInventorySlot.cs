using UnityEngine;
using UnityEngine.EventSystems;

namespace LichLord.UI
{
    public class UIInventorySlot : UIItemSlot
    {
        [SerializeField]
        private int _slotIndex = -1;
        public int SlotIndex => _slotIndex;

        public override void OnEndDrag(PointerEventData eventData)
        {
            DestroyDragPreview();

            if (_itemDefinition == null)
                return;

            GameObject droppedOn = eventData.pointerCurrentRaycast.gameObject;
            UIItemSlot targetSlot = droppedOn?.GetComponent<UIItemSlot>();

            if (targetSlot == null || targetSlot == this)
                return;
            
            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            PlayerInventoryComponent inventory = pc.Inventory;

            if (targetSlot is UIInventorySlot inventorySlot)
            {
                inventory.SetItemAtInventorySlot(inventorySlot.SlotIndex, in _itemData);
                inventory.SetItemAtInventorySlot(_slotIndex, in inventorySlot.ItemData);
            }

            if (targetSlot is UILoadoutSlot loadoutSlot)
            {
                if (_itemDefinition.ValidLoadoutSlots.Contains(loadoutSlot.LoadoutSlot))
                {
                    inventory.SetItemAtLoadoutSlot(loadoutSlot.LoadoutSlot, in _itemData);
                    inventory.SetItemAtInventorySlot(_slotIndex, in loadoutSlot.ItemData);
                }
            }
        }
    }
}
