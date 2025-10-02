using LichLord.Items;
using LichLord.NonPlayerCharacters;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIStrongholdWorkerItemSlot : UIContainerSlot
    {
        [SerializeField]
        protected Image _workerStatusIcon;
        public Image WorkerStatusIcon => _workerStatusIcon;

        [SerializeField]
        private WorkerComponent _workerComponent;
        public WorkerComponent WorkerComponent => _workerComponent;

        [SerializeField]
        private int _workerIndex = -1;
        public int WorkerIndex => _workerIndex;

        public void SetWorkerData(WorkerComponent workerComponent, int workerIndex)
        {
            _workerComponent = workerComponent; 
            _workerIndex = workerIndex;
        }

        public override void SetItemData(FItemData itemData)
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
                _workerStatusIcon.enabled = true;
            }
            else
            {
                _iconImage.enabled = false;
                _countText.enabled = false;
                _workerStatusIcon.enabled = false;
            }
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
                FItemData otherSlotItem = containerSlot.ItemData;
                FItemData thisSlotItem = _itemData;

                Context.ContainerManager.RPC_SetItemSlotData(FullItemSlotIndex, otherSlotItem);
                Context.ContainerManager.RPC_SetItemSlotData(containerSlot.FullItemSlotIndex, thisSlotItem);
            }
        }
    }
}
