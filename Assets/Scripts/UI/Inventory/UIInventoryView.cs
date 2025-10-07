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

        protected override void OnOpen()
        {
            base.OnOpen();

            _inventoryWidget.SetActive(true);

            ERightWidgetType currentType = GetRightWidgetType();

            // --- Deactivate all right-hand widgets ---
            foreach (var widget in _inventoryContextWidget.Values)
                widget.SetActive(false);

            // --- Activate the one that matches currentType ---
            if (_inventoryContextWidget.TryGetValue(currentType, out var activeWidget))
                activeWidget.SetActive(true);
        }

        protected override void OnTick()
        {
            base.OnTick();

            ERightWidgetType currentType = GetRightWidgetType();

            // --- Close UI if the current interactable is gone ---
            if (currentType == ERightWidgetType.None)
            {
                foreach (var kvp in _inventoryContextWidget)
                {
                    var widget = kvp.Value;
                    if (widget.isActiveAndEnabled)
                    {
                        if (Context.UI is GameplayUI gameplayUI)
                            gameplayUI.CloseInventoryWindow();
                        break;
                    }
                }
            }
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
