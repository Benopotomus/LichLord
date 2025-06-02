using Fusion;
using FusionHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord
{
    public class PlayerRelay : ContextBehaviour
    {
                // This is set by FusionSession when the player spawns and should not be used by the application. Use PlayerIndex instead.
        [Networked] public int NetworkedPlayerIndex { private get; set; }

        // These are local properties so they remain valid when the network state goes away (also, they don't change during the life of the NO).
        public PlayerRef PlayerId { get; private set; } = PlayerRef.None;
        public int PlayerIndex { get; private set; } = -1;

        [SerializeField] private TickAlignedEventRelay _eventStub;

        public override void Spawned()
        {
            // Getting this here because it will revert to -1 if the player disconnects, but we still want to remember the Id we were assigned for clean-up purposes
            PlayerId = Object.InputAuthority;
            PlayerIndex = NetworkedPlayerIndex;            

            //RegisterEventListener((DamageEvent evt) => ApplyAreaDamage(evt.impulse, evt.damage));
        }

        private void ApplyAreaDamage(Vector3 impulse, int damage)
        {
            if (!Context.IsGameplayActive())
                return;


        }

        protected void RegisterEventListener<T>(Action<T> listener) where T : unmanaged, INetworkEvent
        {
            _eventStub.RegisterEventListener(listener);
        }

        public void RaiseEvent<T>(T evt) where T : unmanaged, INetworkEvent
        {
            _eventStub.RaiseEventFor(_eventStub, evt);
        }


    }


}
