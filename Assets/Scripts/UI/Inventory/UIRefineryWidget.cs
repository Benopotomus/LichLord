using LichLord.Buildables;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIRefineryWidget : UIInventoryContextWidget
    {
        [SerializeField]
        private Slider _progressSlider;

        [SerializeField] private List<UIContainerSlot> _inItemSlots = new();
        [SerializeField] private List<UIContainerSlot> _outItemSlots = new();

        private int _containerIndex;

        [SerializeField]
        private Refinery _refinery;

        protected override void OnVisible()
        {
            base.OnVisible();

            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return;

            var interactor = pc.Interactor;
            if (interactor == null)
                return;

            var interactable = interactor.CurrentInteractable;
            if (interactable == null)
                return;

            if (interactable.Owner is not Refinery refinery)
                return;

            _refinery = refinery;
            _containerIndex = refinery.RuntimeState.GetContainerIndex();
            _progressSlider.value = _refinery.RuntimeState.GetRefineryProgressPercent();

            RefreshSlots();
        }

        protected override void OnTick()
        {
            base.OnTick();
            RefreshStorageItems();

            _progressSlider.value = _refinery.RuntimeState.GetRefineryProgressPercent();
        }

        private void RefreshSlots()
        {
            if (_refinery == null)
                return;

            var inSlotDatas = _refinery.RuntimeState.GetRefineryInItemSlotDatas();
            var outSlotDatas = _refinery.RuntimeState.GetRefineryOutItemSlotDatas();

            // --- Setup Input Slots ---
            for (int i = 0; i < _inItemSlots.Count; i++)
            {
                if (i < inSlotDatas.Count)
                {
                    var (index, slotData) = inSlotDatas[i];
                    _inItemSlots[i].SetItemSlotData(_containerIndex, index);
                    _inItemSlots[i].SetItemData(slotData.ItemData);
                    _inItemSlots[i].SetActive(true);
                }
                else
                {
                    _inItemSlots[i].SetActive(false);
                }
            }

            // --- Setup Output Slots ---
            for (int i = 0; i < _outItemSlots.Count; i++)
            {
                if (i < outSlotDatas.Count)
                {
                    var (index, slotData) = outSlotDatas[i];
                    _outItemSlots[i].SetItemSlotData(_containerIndex, index);
                    _outItemSlots[i].SetItemData(slotData.ItemData);
                    _outItemSlots[i].SetActive(true);
                }
                else
                {
                    _outItemSlots[i].SetActive(false);
                }
            }
        }

        public void RefreshStorageItems()
        {
            if (_refinery == null)
                return;

            var inSlotDatas = _refinery.RuntimeState.GetRefineryInItemSlotDatas();
            var outSlotDatas = _refinery.RuntimeState.GetRefineryOutItemSlotDatas();

            // --- Refresh Input Slots ---
            for (int i = 0; i < _inItemSlots.Count && i < inSlotDatas.Count; i++)
            {
                _inItemSlots[i].SetItemData(inSlotDatas[i].Item2.ItemData);
            }

            // --- Refresh Output Slots ---
            for (int i = 0; i < _outItemSlots.Count && i < outSlotDatas.Count; i++)
            {
                _outItemSlots[i].SetItemData(outSlotDatas[i].Item2.ItemData);
            }
        }
    }
}
