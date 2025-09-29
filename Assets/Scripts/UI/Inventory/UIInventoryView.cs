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

        protected override void OnOpen()
        {
            base.OnOpen();

            _inventoryWidget.SetActive(true);

            if (ShouldOpenStorageWidget())
            {
                _storageChestWidget.SetActive(true);
                _loadoutWidget.SetActive(false);
            }
            else
            {
                _storageChestWidget.SetActive(false);
                _loadoutWidget.SetActive(true);
            }
        }

        public bool ShouldOpenStorageWidget()
        {
            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return false;

            InteractorComponent interactor = pc.Interactor;
            if (interactor == null)
                return false;

            InteractableComponent interactable = pc.Interactor.CurrentInteractable;
            if (interactable == null)
                return false;

            if (interactable.Owner is StorageChest storageChest)
            { 
                return true;
            }

            return false;
        }
    }
}
