using LichLord.Buildables;
using LichLord.Items;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{ 
    public class UIStorageChestWidget : UIInventoryContextWidget
    {
        [SerializeField] 
        private List<UIContainerSlot> _containerSlots = new List<UIContainerSlot>();

        [SerializeField]
        private int _containerIndex;

        protected override void OnVisible()
        {
            base.OnVisible();

            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return;

            InteractorComponent interactor = pc.Interactor;
            if (interactor == null)
                return;

            InteractableComponent interactable = pc.Interactor.CurrentInteractable;
            if (interactable == null)
                return;

            if (interactable.Owner is StorageChest storageChest)
            {
                _containerIndex = storageChest.RuntimeState.GetContainerIndex();
                FContainerSlotData containerData = Context.ContainerManager.GetContainerDataAtIndex(_containerIndex);
                int itemSlotCount = containerData.EndIndex - containerData.StartIndex;

                List<FItemSlotData> itemSlots = Context.ContainerManager.GetItemSlotDatasFromContainerIndex(_containerIndex);

                for (int i = 0; i < _containerSlots.Count; i++)
                {
                    if (i <= itemSlotCount)
                    {
                        _containerSlots[i].SetItemSlotData(_containerIndex, containerData.StartIndex + i);
                        _containerSlots[i].SetItemData(itemSlots[i].ItemData);
                        _containerSlots[i].SetActive(true);
                    }
                    else
                    {
                        _containerSlots[i].SetActive(false);
                    }
                }
            }
        }

        private void OnClosePressed()
        {
            if (Context.UI is GameplayUI gameplayUI)
            {
                gameplayUI.CloseInventoryWindow();
            }
        }

        protected override void OnTick()
        {
            base.OnTick();

            RefreshStorageItems();
        }

        public void RefreshStorageItems()
        {
            FContainerSlotData containerData = Context.ContainerManager.GetContainerDataAtIndex(_containerIndex);
            if (!containerData.IsAssigned)
                return;

            int itemSlotCount = (containerData.EndIndex - containerData.StartIndex);

            List<FItemSlotData> itemSlots = Context.ContainerManager.GetItemSlotDatasFromContainerIndex(_containerIndex);

            for (int i = 0; i < _containerSlots.Count; i++)
            {
                if (i <= itemSlotCount)
                    _containerSlots[i].SetItemData(itemSlots[i].ItemData);
            }
        }
    }
}
