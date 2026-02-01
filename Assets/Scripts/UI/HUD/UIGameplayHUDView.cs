namespace LichLord.UI
{
    using UnityEngine;

    public class UIGameplayHUDView : UIGameplayView
    {
        [SerializeField]
        private UIFloatingInteract _floatingInteract;
        public UIFloatingInteract FloatingInteract => _floatingInteract;

        [SerializeField]
        private UIStrongholdTracker _strongholdTracker;

        [SerializeField]
        private UIInvasion _invasion;

        [SerializeField]
        private UIWarningText _warningText;

        protected override void OnVisible()
        {
            base.OnVisible();

            Context.StrongholdManager.onStrongholdSpawned += _strongholdTracker.OnStrongholdSpawned;
            Context.StrongholdManager.onStrongholdDespawned += _strongholdTracker.OnStrongholdDespawned;

            foreach(var stronghold in Context.StrongholdManager.ActiveStrongholds)
                _strongholdTracker.OnStrongholdSpawned(stronghold);

            _warningText.SetActive(false);
        }

        protected override void OnTick()
        {
            base.OnTick();

            int tick = Context.Runner.Tick;

            _invasion.SetActive(Context.InvasionManager.InvasionID > 0);

            if (_warningText.isActiveAndEnabled)
            {
                if (tick > _warningText.TimeoutTick)
                {
                    _warningText.SetActive(false);
                }
            }
        }

        public void ShowWarningText(string text, string subtext)
        { 
            _warningText.ShowWarningText(text, subtext);
            _warningText.SetActive(true);
        }
    }
}