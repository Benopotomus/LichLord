namespace LichLord.UI
{
    using LichLord.Props;
    using System;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class UIGameplayHUDView : UIGameplayView
    {
        [SerializeField]
        private UIFloatingInteract _floatingInteract;
        public UIFloatingInteract FloatingInteract => _floatingInteract;

        [SerializeField]
        private UINexusTracker _nexusTracker;
        public UINexusTracker NexusTracker => _nexusTracker;

        protected override void OnVisible()
        {
            base.OnVisible();

            Context.StrongholdManager.onNexusSpawned += _nexusTracker.OnNexusSpawned;
            Context.StrongholdManager.onNexusDespawned += _nexusTracker.OnNexusDespawned;

            foreach(var nexus in Context.StrongholdManager.ActiveNexuses)
                _nexusTracker.OnNexusSpawned(nexus);
        }
    }
}