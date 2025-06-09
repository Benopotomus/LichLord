using System;
using Fusion;
using FusionHelpers;
using UnityEngine;

namespace LichLord
{
    /// <summary>
    /// Base class for an object that will exist in exactly one instance per player.
    /// This *could* be the players avatar (visual game object), but it need not be - it's perfectly valid to just treat this as a data placeholder for the player.
    /// Implement the abstract InitNetworkState to initialize networked data on the State Authority.
    /// </summary>

    public class RelayPlayer : ContextBehaviour
    {
        [SerializeField]
        [Networked] public TickAlignedEventRelay EventRelay { get; set; }

        public override void Spawned()
        {
            // Getting this here because it will revert to -1 if the player disconnects, but we still want to remember the Id we were assigned for clean-up purposes
            RegisterEventListener((PropDamageEvent evt) => ApplyDamageToProp(evt.guid, evt.impulse, evt.damage));
        }

        protected void RegisterEventListener<T>(Action<T> listener) where T : unmanaged, INetworkEvent
        {
            EventRelay.RegisterEventListener(listener);
        }

        public void RaiseEvent<T>(T evt) where T : unmanaged, INetworkEvent
        {
            RelayPlayer stateAuth = Runner.GetPlayerObject(Runner.LocalPlayer).GetComponent<RelayPlayer>();
            stateAuth.EventRelay.RaiseEventFor(EventRelay, evt);
        }

        //20
        private void ApplyDamageToProp(int guid, Vector3 impulse, int damage)
        {
            Context.PropManager.ApplyDamage(guid, impulse, damage);
        }

    }
}