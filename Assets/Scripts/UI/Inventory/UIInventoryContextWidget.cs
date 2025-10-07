
using UnityEngine;

namespace LichLord.UI
{
    public class UIInventoryContextWidget : UIWidget
    {
        [SerializeField] private UIButton _closeButton;

        public void Awake()
        {
            _closeButton.onClick.AddListener(OnClosePressed);
        }

        private void OnClosePressed()
        {
            if (Context.UI is GameplayUI gameplayUI)
            {
                gameplayUI.CloseInventoryWindow();
            }
        }
    }
}
