namespace LichLord.UI
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class UIGameplayHUDView : UIGameplayView
    {
        [SerializeField]
        private UIFloatingInteract _floatingInteract;
        public UIFloatingInteract FloatingInteract => _floatingInteract;

        protected override void OnTick()
        {


        }
    }
}