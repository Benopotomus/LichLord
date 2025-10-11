using AYellowpaper.SerializedCollections;
using LichLord.Buildables;
using UnityEngine;
using System.Linq;

namespace LichLord.UI
{
    public class UIInventoryView : UIGameplayView
    {
        [SerializeField] private UIInventoryWidget _inventoryWidget;

        [SerializeField, SerializedDictionary("RightWidgetType", "Widget")]
        private SerializedDictionary<ERightWidgetType, UIInventoryContextWidget> _inventoryContextWidget;

        private ERightWidgetType _lastWidgetType = ERightWidgetType.None;

        protected override void OnOpen()
        {
            base.OnOpen();

            _inventoryWidget.SetActive(true);

            var currentType = GetRightWidgetType();
            _lastWidgetType = currentType;

            UpdateActiveWidget(currentType);
        }

        protected override void OnTick()
        {
            base.OnTick();

            var currentType = GetRightWidgetType();

            // --- Only run if type changed ---
            if (currentType == _lastWidgetType)
                return;

            // --- Handle closing if current became None ---
            if (currentType == ERightWidgetType.None)
            {
                foreach (var widget in _inventoryContextWidget.Values)
                {
                    if (widget != null && widget.isActiveAndEnabled)
                    {
                        if (Context.UI is GameplayUI gameplayUI)
                            gameplayUI.CloseInventoryWindow();
                        break;
                    }
                }
            }
            else
            {
                // --- Switch to new widget ---
                UpdateActiveWidget(currentType);
            }

            _lastWidgetType = currentType;
        }

        private void UpdateActiveWidget(ERightWidgetType currentType)
        {
            // Deactivate all
            foreach (var widget in _inventoryContextWidget.Values)
                widget.SetActive(false);

            // Activate current
            if (_inventoryContextWidget.TryGetValue(currentType, out var activeWidget))
                activeWidget.SetActive(true);
        }

        private ERightWidgetType GetRightWidgetType()
        {
            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return ERightWidgetType.Loadout;

            var interactor = pc.Interactor;
            if (interactor == null)
                return ERightWidgetType.Loadout;

            var interactable = interactor.CurrentInteractable;
            if (interactable == null)
                return ERightWidgetType.Loadout;

            return interactable.Owner switch
            {
                StorageChest => ERightWidgetType.StorageChest,
                Stronghold => ERightWidgetType.Stronghold,
                Stockpile => ERightWidgetType.Stockpile,
                Refinery => ERightWidgetType.IronRefinery,
                _ => ERightWidgetType.None
            };
        }

        public enum ERightWidgetType
        {
            None,
            Loadout,
            StorageChest,
            Stronghold,
            Stockpile,
            IronRefinery,
        }
    }
}
