using UnityEngine;

namespace LichLord.UI
{ 
    public class UIInventoryWidget : UIWidget
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
                gameplayUI.Close(gameplayUI.InventoryView);
            }
        }
    }
}
