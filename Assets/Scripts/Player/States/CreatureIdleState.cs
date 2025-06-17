using Fusion.Addons.FSM;
using UnityEngine;

namespace LichLord
{
    [Tooltip("The Idle state of the player.")]
    public class CreatureIdleState : CreatureStateBase
    {
        protected override void OnEnterStateRender()
        {
            //anim.CrossFadeInFixedTime(animState, 4f / 60f);
        }

        protected override void OnFixedUpdate()
        {
            FGameplayInput input = fsmRef.Creature.Input.CurrentInput;

            fsmRef.Creature.Movement.OnFixedUpdate(ref input);
            fsmRef.Creature.Maneuvers.OnFixedUpdate(ref input);
            fsmRef.Creature.CameraController.OnFixedUpdate(ref input);

            fsmRef.Creature.Input.ResetInput();
        }

        protected override void OnRender()
        {
            float deltaTime = Time.deltaTime;

            fsmRef.Creature.Movement.OnRender(deltaTime);
            fsmRef.Creature.Maneuvers.OnRender();
        }
    }
}