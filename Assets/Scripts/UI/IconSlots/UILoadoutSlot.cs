using UnityEngine;
using UnityEngine.EventSystems;

namespace LichLord.UI
{
    public class UILoadoutSlot : UIItemSlot 
    {
        [SerializeField]
        private ELoadoutSlot _loadoutSlot = ELoadoutSlot.None;
        public ELoadoutSlot LoadoutSlot =>  _loadoutSlot;

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

            // Check if you can drop the item in the slot

            if (targetSlot is UIInventorySlot inventorySlot)
            {
                inventory.SetItemAtInventorySlot(inventorySlot.SlotIndex, in _itemData);
                inventory.SetItemAtLoadoutSlot(LoadoutSlot, in inventorySlot.ItemData);
            }

            if (targetSlot is UILoadoutSlot loadoutSlot)
            {
                if (_itemDefinition.ValidLoadoutSlots.Contains(loadoutSlot.LoadoutSlot))
                {
                    inventory.SetItemAtLoadoutSlot(loadoutSlot.LoadoutSlot, in _itemData);
                    inventory.SetItemAtLoadoutSlot(LoadoutSlot, in loadoutSlot.ItemData);
                }
            }
        }
    }
}
