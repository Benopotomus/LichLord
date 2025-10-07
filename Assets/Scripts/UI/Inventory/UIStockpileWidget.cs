using LichLord.Buildables;
using LichLord.Items;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{ 
    public class UIStockpileWidget : UIInventoryContextWidget
    {
        [SerializeField] 
        private List<UIStockpileSlot> _stockpileItemSlot = new List<UIStockpileSlot>();

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

            if (interactable.Owner is Stockpile stockpile)
            {
                _containerIndex = stockpile.RuntimeState.GetContainerIndex();
                FContainerSlotData containerData = Context.ContainerManager.GetContainerDataAtIndex(_containerIndex);
                int itemSlotCount = containerData.EndIndex - containerData.StartIndex;

                List<FItemSlotData> itemSlots = Context.ContainerManager.GetItemSlotDatasFromContainerIndex(_containerIndex);

                for (int i = 0; i < _stockpileItemSlot.Count; i++)
                {
                    if (i <= itemSlotCount)
                    {
                        _stockpileItemSlot[i].SetItemSlotData(_containerIndex, containerData.StartIndex + i);
                        _stockpileItemSlot[i].SetItemData(itemSlots[i].ItemData);
                        _stockpileItemSlot[i].SetActive(true);
                    }
                    else
                    {
                        _stockpileItemSlot[i].SetActive(false);
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

            for (int i = 0; i < _stockpileItemSlot.Count; i++)
            {
                if (i <= itemSlotCount)
                {
                    _stockpileItemSlot[i].SetItemData(itemSlots[i].ItemData);


                }
            }
        }
    }
}
