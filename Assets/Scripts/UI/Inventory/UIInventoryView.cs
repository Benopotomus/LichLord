using LichLord.Buildables;
using UnityEngine;

namespace LichLord.UI
{
    public class UIInventoryView : UIGameplayView
    {
        [SerializeField]
        private UIInventoryWidget _inventoryWidget;

        [SerializeField]
        private UILoadoutWidget _loadoutWidget;

        [SerializeField]
        private UIStorageChestWidget _storageChestWidget;

        [SerializeField]
        private UIStrongholdContainerWidget _strongholdContainerWidget;

        [SerializeField]
        private UIStockpileWidget _stockpileWidget;

        protected override void OnOpen()
        {
            base.OnOpen();

            _inventoryWidget.SetActive(true);

            switch (GetRightWidgetType())
            {
                case ERightWidgetType.None:
                    _loadoutWidget.SetActive(false);
                    _storageChestWidget.SetActive(false);
                    _strongholdContainerWidget.SetActive(false);
                    _stockpileWidget.SetActive(false);
                    break;
                case ERightWidgetType.Loadout:
                    _loadoutWidget.SetActive(true);
                    _storageChestWidget.SetActive(false);
                    _strongholdContainerWidget.SetActive(false);
                    _stockpileWidget.SetActive(false);
                    break;
                case ERightWidgetType.StorageChest:
                    _storageChestWidget.SetActive(true);
                    _loadoutWidget.SetActive(false);
                    _strongholdContainerWidget.SetActive(false);
                    _stockpileWidget.SetActive(false);
                    break;
                case ERightWidgetType.Stronghold:
                    _strongholdContainerWidget.SetActive(true);
                    _storageChestWidget.SetActive(false);
                    _loadoutWidget.SetActive(false);
                    _stockpileWidget.SetActive(false);
                    break;
                case ERightWidgetType.Stockpile:
                    _stockpileWidget.SetActive(true);
                    _storageChestWidget.SetActive(false);
                    _loadoutWidget.SetActive(false);
                    _strongholdContainerWidget.SetActive(false);
                    break;
            }
        }

        protected override void OnTick()
        {
            base.OnTick();

            switch (GetRightWidgetType())
            {
                case ERightWidgetType.None:
                    if (_storageChestWidget.isActiveAndEnabled)
                    {
                        if (Context.UI is GameplayUI gameplayUI)
                            gameplayUI.CloseInventoryWindow();
                    }

                    if (_strongholdContainerWidget.isActiveAndEnabled)
                    {
                        if (Context.UI is GameplayUI gameplayUI)
                            gameplayUI.CloseInventoryWindow();
                    }

                    break;
            }
        }

        private ERightWidgetType GetRightWidgetType()
        {
            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return ERightWidgetType.Loadout;

            InteractorComponent interactor = pc.Interactor;
            if (interactor == null)
                return ERightWidgetType.Loadout;

            InteractableComponent interactable = pc.Interactor.CurrentInteractable;
            if (interactable == null)
                return ERightWidgetType.Loadout;

            if (interactable.Owner is StorageChest storageChest)
                return ERightWidgetType.StorageChest;
            else if (interactable.Owner is Stronghold stronghold)
                return ERightWidgetType.Stronghold;
            else if (interactable.Owner is Stockpile stockpile)
                return ERightWidgetType.Stockpile;

                return ERightWidgetType.None;
        }

        public enum ERightWidgetType
        { 
            None,
            Loadout,
            StorageChest,
            Stronghold,
            Stockpile,
        }
    }
}
