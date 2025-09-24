
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

            if (Context.LocalPlayerCharacter.Input.CurrentInput.InventoryToggle)
            {
                if(_inventoryView.IsOpen) 
                    _inventoryView.Close();
                else
                    _inventoryView.Open();
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
