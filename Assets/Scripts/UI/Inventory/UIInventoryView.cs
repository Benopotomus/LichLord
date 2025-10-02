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

        protected override void OnOpen()
        {
            base.OnOpen();

            _inventoryWidget.SetActive(true);

            switch (GetContainerTypeWidget())
            {
                case EContainerWidgetType.None:
                    _loadoutWidget.SetActive(true);
                    _storageChestWidget.SetActive(false);
                    _strongholdContainerWidget.SetActive(false);
                    break;
                case EContainerWidgetType.Storage:
                    _storageChestWidget.SetActive(true);
                    _loadoutWidget.SetActive(false);
                    _strongholdContainerWidget.SetActive(false);
                    break;
                case EContainerWidgetType.Stronghold:
                    _strongholdContainerWidget.SetActive(true);
                    _storageChestWidget.SetActive(false);
                    _loadoutWidget.SetActive(false);
                    break;
            }
        }

        protected override void OnTick()
        {
            base.OnTick();

            switch (GetContainerTypeWidget())
            {
                case EContainerWidgetType.None:
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

        private EContainerWidgetType GetContainerTypeWidget()
        {
            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return EContainerWidgetType.None;

            InteractorComponent interactor = pc.Interactor;
            if (interactor == null)
                return EContainerWidgetType.None;

            InteractableComponent interactable = pc.Interactor.CurrentInteractable;
            if (interactable == null)
                return EContainerWidgetType.None;

            if (interactable.Owner is StorageChest storageChest)
                return EContainerWidgetType.Storage;
            else if (interactable.Owner is Stronghold stronghold)
                return EContainerWidgetType.Stronghold;

            return EContainerWidgetType.None;
        }

        public enum EContainerWidgetType
        { 
            None,
            Storage,
            Stronghold,
        }
    }
}
