
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

            if (Context.DialogTarget != null)
            {
                _dialogView.Open();
            }
            else
            {
                _dialogView.Close();
            }
        }

        protected override void OnViewOpened(UIView view)
        {
            base.OnViewOpened(view);
        }

        protected override void OnViewClosed(UIView view)
        {
            base.OnViewClosed(view);
        }

        public void SetLastFocusWidget()
        {
            LastFocusedWidget = EventSystem.current.currentSelectedGameObject;
        }

    }
}
