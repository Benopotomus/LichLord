using LichLord.Items;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LichLord.UI
{
    public class UIContainerSlot : UIDraggableItemSlot
    {
        [SerializeField]
        private int _containerIndex = -1;
        public int ContainerIndex => _containerIndex;

        [SerializeField]
        private int _fullItemSlotIndex = -1;
        public int FullItemSlotIndex => _fullItemSlotIndex;

        public void SetItemSlotData(int containerIndex, int fullItemSlotIndex)
        { 
            _containerIndex = containerIndex;
            _fullItemSlotIndex = fullItemSlotIndex;
        }

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
                FItemData inventorySlotItem = inventorySlot.ItemData;
                FItemData thisSlotItem = _itemData;

                Context.ContainerManager.RPC_SetItemSlotData(FullItemSlotIndex, inventorySlotItem);
                inventory.SetItemAtInventorySlot(inventorySlot.SlotIndex, in thisSlotItem);
            }

            if (targetSlot is UIContainerSlot containerSlot)
            {
                if (targetSlot is UIRefineryOutSlot outSlot)
                    return;

                FItemData otherSlotItem = containerSlot.ItemData;
                FItemData thisSlotItem = _itemData;

                Context.ContainerManager.RPC_StackOrSwapItemsAtSlots((ushort)FullItemSlotIndex, (ushort)containerSlot.FullItemSlotIndex);

            }
        }
    }
}
