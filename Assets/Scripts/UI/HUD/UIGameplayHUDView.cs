namespace LichLord.UI
{
    using UnityEngine;

    public class UIGameplayHUDView : UIGameplayView
    {
        [SerializeField]
        private UIFloatingInteract _floatingInteract;
        public UIFloatingInteract FloatingInteract => _floatingInteract;

        [SerializeField]
        private UIFloatingTooltip _floatingTooltip;
        public UIFloatingTooltip FloatingTooltip => _floatingTooltip;

        [SerializeField]
        private UIStrongholdTracker _strongholdTracker;

        [SerializeField]
        private UIInvasion _invasion;

        protected override void OnVisible()
        {
            base.OnVisible();

            Context.StrongholdManager.onStrongholdSpawned += _strongholdTracker.OnStrongholdSpawned;
            Context.StrongholdManager.onStrongholdDespawned += _strongholdTracker.OnStrongholdDespawned;

            foreach(var stronghold in Context.StrongholdManager.ActiveStrongholds)
                _strongholdTracker.OnStrongholdSpawned(stronghold);
        }

        protected override void OnTick()
        {
            base.OnTick();

            _invasion.SetActive(Context.InvasionManager.InvasionID > 0);
        }
    }
}