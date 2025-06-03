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
        [SerializeField] TickAlignedEventRelay _eventRelayPrefab;

        // This is set by FusionSession when the player spawns and should not be used by the application. Use PlayerIndex instead.
        [Networked] public int NetworkedPlayerIndex { private get; set; }

        [SerializeField]
        [Networked] private TickAlignedEventRelay eventRelay { get; set; }

        // These are local properties so they remain valid when the network state goes away (also, they don't change during the life of the NO).
        public PlayerRef PlayerId { get; private set; } = PlayerRef.None;
        public int PlayerIndex { get; private set; } = -1;

        [SerializeField]
        private TickAlignedEventRelay _eventStub;

        public override void Spawned()
        {
            // Getting this here because it will revert to -1 if the player disconnects, but we still want to remember the Id we were assigned for clean-up purposes
            PlayerId = Object.InputAuthority;
            PlayerIndex = NetworkedPlayerIndex;

            // The EventRelay only makes sense in shared mode, and most games don't support both hosted and shared,
            // so this particular check is very specific to Tanknarok.
            if (Runner.Topology == Topologies.Shared)
            {
                if (HasStateAuthority)
                    eventRelay = Runner.Spawn(_eventRelayPrefab);
                _eventStub = eventRelay;
                _eventStub.transform.SetParent(transform);
            }

            Debug.Log($"Spawned Player with InputAuth {PlayerId}, Index {PlayerIndex}");
            RegisterEventListener((DamageEvent evt) => ApplyDamageToProp(evt.guid, evt.impulse, evt.damage));
        }

        private void ApplyDamageToProp(int guid, Vector3 impulse, int damage)
        {
            Debug.Log("Relay Player Got the message");
            Context.PropManager.ApplyDamage(guid, impulse, damage);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Debug.Log($"Despawned Player with InputAuth {PlayerId}, Index {PlayerIndex}");
        }

        protected void RegisterEventListener<T>(Action<T> listener) where T : unmanaged, INetworkEvent
        {
            _eventStub.RegisterEventListener(listener);
        }

        public void RaiseEvent<T>(T evt) where T : unmanaged, INetworkEvent
        {
            RelayPlayer stateAuth = Runner.GetPlayerObject(Runner.LocalPlayer).GetComponent<RelayPlayer>();
            stateAuth._eventStub.RaiseEventFor(_eventStub, evt);
        }
    }
}