
namespace LichLord.UI
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;

    public class GameplayUI : SceneUI
    {
        [SerializeField] private UIGameplayHUDView _gameplayHUD;
        public UIGameplayHUDView HUD => _gameplayHUD;

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
