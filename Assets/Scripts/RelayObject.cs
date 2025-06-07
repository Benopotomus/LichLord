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

        [Networked]
        private int Health { get; set; } = 1000;

        public override void Spawned()
        {
                        base.Spawned();
            RegisterEventListener((NonPlayerCharacterDamageEvent evt) => ApplyDamageToNPC(evt.guid, evt.impulse, evt.damage));
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

        private void ApplyDamageToNPC(int guid, Vector3 impulse, int damage)
        {
            Debug.Log("Relay Hit");
            Health -= damage;

            Context.NonPlayerCharacterManager.ApplyDamage(guid, impulse, damage);
        }
    }
}