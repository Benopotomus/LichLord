using System;
using Fusion;
using FusionHelpers;
using UnityEngine;

namespace LichLord
{
    public class RelayObject : ContextBehaviour
    {
        [SerializeField]
        [Networked] public TickAlignedEventRelay EventRelay { get; set; }

        public override void Spawned()
        {
            base.Spawned();

        }

        public override void Render()
        {
            //Debug.Log(Health);
            //EventRelay.DebugPrintCurrentEvents();
            base.Render();
        }

        protected void RegisterEventListener<T>(Action<T> listener) where T : unmanaged, INetworkEvent
        {
            EventRelay.RegisterEventListener(listener);
        }

        public void RaiseEvent<T>(T evt) where T : unmanaged, INetworkEvent
        {
            Debug.Log("Raise Event");
            RelayPlayer stateAuth = Runner.GetPlayerObject(Runner.LocalPlayer).GetComponent<RelayPlayer>();
            stateAuth.EventRelay.RaiseEventFor(EventRelay, evt);
        }
    }
}