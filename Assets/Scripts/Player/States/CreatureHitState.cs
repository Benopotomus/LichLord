
namespace LichLord
{
    using Fusion.Addons.FSM;
    using UnityEngine;

    public class CreatureHitState : CreatureStateBase
    {
        [SerializeField, Tooltip("The knockback speed the player enters after being damaged.")]
        Vector2 speed = new Vector2(12.5f, 10f);

        [SerializeField, Tooltip("How much time must pass before returning to the idle state.")]
        public float delayTime;

        public override void Spawned()
        {
            fsmRef = GetComponentInParent<CreatureFSM>();
            base.Spawned();
        }

        protected override void OnEnterStateRender()
        {
            // When the player has state authority, this effect is played instantly in FixedUpdateNetworked; however, for other players, the effect will player when entering this state.
            if (!HasStateAuthority)
            {
                //fsmRef.PlayerNetworkObject.damageFX.PlayFX();
            }

            anim.CrossFadeInFixedTime(animState, animTransitionLength);
            base.OnEnterStateRender();
        }

        protected override void OnEnterState()
        {
            base.OnEnterState();
        }

        protected override void OnFixedUpdate()
        {

        }
    }
}