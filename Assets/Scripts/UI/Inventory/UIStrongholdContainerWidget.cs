using LichLord.Items;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{ 
    public class UIStrongholdContainerWidget : UIInventoryContextWidget
    {
        [SerializeField] 
        private List<UIStrongholdWorkerItemSlot> _workerItemSlots = new List<UIStrongholdWorkerItemSlot>();

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

            if (interactable.Owner is Stronghold stronghold)
            {
                _containerIndex = stronghold.ContainerIndex;
                FContainerSlotData containerData = Context.ContainerManager.GetContainerDataAtIndex(_containerIndex);
                int itemSlotCount = containerData.EndIndex - containerData.StartIndex;

                List<FItemSlotData> itemSlots = Context.ContainerManager.GetItemSlotDatasFromContainerIndex(_containerIndex);

                var workerComponent = stronghold.WorkerComponent;

                for (int i = 0; i < _workerItemSlots.Count; i++)
                {
                    if (i >= workerComponent.MaxWorkerCount)
                    {
                        _workerItemSlots[i].SetActive(false);
                        continue;
                    }

                    if (i <= itemSlotCount)
                    {
                        _workerItemSlots[i].SetItemSlotData(_containerIndex, containerData.StartIndex + i);
                        _workerItemSlots[i].SetItemData(itemSlots[i].ItemData);
                        _workerItemSlots[i].SetWorkerData(stronghold.WorkerComponent, i);
                        _workerItemSlots[i].SetActive(true);
                    }
                    else
                    {
                        _workerItemSlots[i].SetActive(false);
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

            for (int i = 0; i < _workerItemSlots.Count; i++)
            {
                if (i <= itemSlotCount)
                {
                    _workerItemSlots[i].SetItemData(itemSlots[i].ItemData);


                }
            }
        }
    }
}
