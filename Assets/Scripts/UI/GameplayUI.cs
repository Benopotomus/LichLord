
namespace LichLord.UI
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class GameplayUI : SceneUI
    {
        [SerializeField] private UIGameplayHUDView _gameplayHUD;
        public UIGameplayHUDView HUD => _gameplayHUD;

        [SerializeField] private UIDialogView _dialogView;
        public UIDialogView DialogView => _dialogView;

        [SerializeField] private UIInventoryView _inventoryView;
        public UIInventoryView InventoryView => _inventoryView;

        [Space]
        public GameObject LastFocusedWidget;

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
        }

        protected override void OnTickInternal()
        {
            base.OnTickInternal();

            if (Context.DialogManager.LocalActiveDialogNode != null)
            {
                _dialogView.SetDialogNode(Context.DialogManager.LocalActiveDialogNode);
                _dialogView.Open();
            }
            else
            {
                _dialogView.Close();
            }

            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc != null)
            {
                if (pc.Input.CurrentInput.InventoryToggle)
                {
                    if (_inventoryView.IsOpen)
                        CloseInventoryWindow();
                    else
                        _inventoryView.Open();
                }

                if (pc.Input.CurrentInput.Cancel)
                {
                    if (_inventoryView.IsOpen)
                        CloseInventoryWindow();
                }

                if (pc.Interactor.CurrentInteractable != null)
                {
                    if (pc.Interactor.InteractType == EInteractType.Container ||
                        pc.Interactor.InteractType == EInteractType.Stronghold ||
                        pc.Interactor.InteractType == EInteractType.Refinery) 
                    {
                        if (!_inventoryView.IsOpen)
                        {
                            _inventoryView.Open();
                        }
                        else
                        { 
                            if (pc.Input.CurrentInput.UI_Interact)
                                CloseInventoryWindow();
                        }
                        
                    }
                }
            }
        }

        public void CloseInventoryWindow()
        {
            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return;

            if (_inventoryView.IsOpen)
                _inventoryView.Close();

            if (pc.Interactor.InteractType == EInteractType.Container ||
                pc.Interactor.InteractType == EInteractType.Stronghold ||
                pc.Interactor.InteractType == EInteractType.Refinery)
            {
                pc.Interactor.SetInteractType(EInteractType.None);
            }
        }

        protected override void OnViewOpened(UIView view)
        {
            base.OnViewOpened(view);

            if (view is UIGameplayView gameplayView)
            { 
                if(gameplayView.UnlocksCursorWhileOpen)
                    Cursor.lockState = CursorLockMode.None;
            }
        }

        protected override void OnViewClosed(UIView view)
        {
            base.OnViewClosed(view);
            bool hasUnlockingView = false;

            foreach (UIView currenvView in _views)
            {
                if (view is UIGameplayView gameplayView)
                {
                    if (gameplayView.IsOpen && gameplayView.UnlocksCursorWhileOpen)
                    { 
                        hasUnlockingView = true; 
                        break;
                    }
                }
            }

            if(!hasUnlockingView)
                Cursor.lockState = CursorLockMode.Locked;
        }

        public void SetLastFocusWidget()
        {
            LastFocusedWidget = EventSystem.current.currentSelectedGameObject;
        }


    }
}
