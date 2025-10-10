using LichLord.Items;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LichLord.UI
{
    public class UIInventorySlot : UIDraggableItemSlot
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
                FItemData refund = inventory.StackItem(inventorySlot.SlotIndex, ref _itemData);
                inventory.SetItemAtInventorySlot(_slotIndex, in refund);
            }

            if (targetSlot is UIContainerSlot containerSlot)
            {
                if (targetSlot is UIRefineryOutSlot outSlot)
                    return;

                if (targetSlot is UIStrongholdWorkerItemSlot workerItemSlot)
                {
                    if (_itemDefinition is not SummonableDefinition summonableDefinition)
                        return;
                }

                if (targetSlot is UIStockpileSlot stockpileSlot)
                {
                    if (_itemDefinition is not CurrencyDefinition currencyDefinition)
                        return;

                    switch (currencyDefinition.CurrencyType)
                    {
                        case ECurrencyType.Wood:
                        case ECurrencyType.Stone:
                        case ECurrencyType.IronOre:
                        case ECurrencyType.Deathcaps:
                        case ECurrencyType.IronBar:
                            break;
                        default:
                            return;
                    }
                }

                FItemData containerSlotItem = containerSlot.ItemData;
                Context.ContainerManager.RPC_StackOrSwapItemAtSlot((byte)pc.PlayerIndex, (ushort)containerSlot.FullItemSlotIndex, _itemData);
                inventory.SetItemAtInventorySlot(_slotIndex, new FItemData());
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
