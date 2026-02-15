using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

namespace LichLord
{
    public class CreatureDeathState : CharacterStateBase
    {
        [Tooltip("The NetworkObject that will be spawned when the player dies.")]
        public NetworkObject deadPlayerSpawn;

        [Tooltip("The renderers that will be hidden when a player dies.")]
        public Renderer[] rendererObjects;

        [Tooltip("The amount of time in seconds the player must wait before respawning at the start point.")]
        public float respawnTime;

        [Tooltip("The respawn position in world space.")]
        public Vector3 respawnPosition = Vector3.up * 2f;

        protected override void OnEnterStateRender()
        {
            foreach (var renderer in rendererObjects)
                renderer.gameObject.SetActive(false);

            if (!HasStateAuthority)
            {
                //fsmRef.PlayerNetworkObject.damageFX.PlayFX();
            }
        }

        protected override void OnExitStateRender()
        {
            foreach (var renderer in rendererObjects)
                renderer.gameObject.SetActive(true);
        }

        protected override void OnEnterState()
        {
            Runner.Spawn(deadPlayerSpawn, transform.position);
            base.OnEnterState();
        }

        protected override void OnFixedUpdate()
        {
            if (Machine.StateTime >= respawnTime)
            {
                //fsmRef.PlayerNetworkObject.Health = fsmRef.PlayerNetworkObject.MaxHealth;
                Machine.TryActivateState<SpellcastState>();
            }
        }
    }
}