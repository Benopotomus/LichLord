// This is the base for UIViews in the LevelScene

namespace LichLord.UI
{
    using UnityEngine.EventSystems;

    public class UIGameplayView : UIView
    {
        public bool BlocksHeroInput;

        protected void ReturnFocusToLastWidget()
        {
            GameplayUI gameplayUI = Context.UI as GameplayUI;

            if (gameplayUI != null)
            {
                EventSystem.current.SetSelectedGameObject(gameplayUI.LastFocusedWidget);
            }
        }

        protected void SetLastFocusWidget()
        {
            GameplayUI gameplayUI = Context.UI as GameplayUI;

            if (gameplayUI != null)
            {
                gameplayUI.SetLastFocusWidget();
            }
        }
    }
}
