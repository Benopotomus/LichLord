using LichLord.Buildables;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIRefineryWidget : UIInventoryContextWidget
    {
        [SerializeField]
        private TextMeshProUGUI _refineryNameText;

        [SerializeField]
        private Slider _progressSlider;

        [SerializeField]
        private List<UIContainerSlot> _inItemSlots = new();

        [SerializeField]
        private List<UIRefineryOutSlot> _outItemSlots = new();

        [SerializeField]
        private VerticalLayoutGroup _recipeContainer;

        [SerializeField]
        private UIRecipeSlot _recipeSlotPrefab;

        [SerializeField]
        private UIRecipeSlot[] _recipeSlots;

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

            BuildableRuntimeState runtimeState = refinery.RuntimeState;

            if (runtimeState.Definition is not RefineryDefinition definition)
                return;

            _refineryNameText.text = definition.BuildableName;
            _containerIndex = refinery.RuntimeState.GetContainerIndex();
            _progressSlider.value = _refinery.RefineryStateComponent.GetLocalRefineryProgress();

            RefreshItemSlots();
            RefreshRecipeSlots(definition);
        }

        protected override void OnTick()
        {
            base.OnTick();
            RefreshStorageItems();

            _progressSlider.value = _refinery.RefineryStateComponent.GetLocalRefineryProgress();
        }

        private void RefreshItemSlots()
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

        private void RefreshRecipeSlots(RefineryDefinition refineryDefinition)
        {
            // Handle null checks
            if (_recipeContainer == null || _recipeSlotPrefab == null)
            {
                Debug.LogError("RecipeContainer or RecipeSlotPrefab is not assigned.");
                return;
            }
            if (refineryDefinition?.RecipeList?.Recipes == null)
            {
                Debug.LogError("RecipeList or Recipes is null.");
                return;
            }

            // Manage existing slots
            for (int i = 0; i < _recipeSlots.Length; i++)
            {
                if (i < refineryDefinition.RecipeList.Recipes.Length && _recipeSlots[i] != null)
                {
                    // Populate and activate slot
                    _recipeSlots[i].SetRecipe(refineryDefinition.RecipeList.Recipes[i]);
                    _recipeSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    // Deactivate excess or null slots
                    if (_recipeSlots[i] != null)
                    {
                        _recipeSlots[i].gameObject.SetActive(false);
                    }
                }
            }

            // If more recipes than slots, create a new array and instantiate additional slots
            if (refineryDefinition.RecipeList.Recipes.Length > _recipeSlots.Length)
            {
                UIRecipeSlot[] newSlots = new UIRecipeSlot[refineryDefinition.RecipeList.Recipes.Length];
                // Copy existing slots
                for (int i = 0; i < _recipeSlots.Length; i++)
                {
                    newSlots[i] = _recipeSlots[i];
                }
                // Instantiate new slots
                for (int i = _recipeSlots.Length; i < refineryDefinition.RecipeList.Recipes.Length; i++)
                {
                    var recipe = refineryDefinition.RecipeList.Recipes[i];
                    var recipeWidget = Instantiate(_recipeSlotPrefab, _recipeContainer.transform);
                    recipeWidget.SetRecipe(recipe);
                    newSlots[i] = recipeWidget;
                }
                // Update the array
                _recipeSlots = newSlots;
            }
        }
    }
}