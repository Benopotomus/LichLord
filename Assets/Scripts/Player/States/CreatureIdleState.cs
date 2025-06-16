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
            fsmRef.Creature.Movement.ProcessInput(input);
            fsmRef.Creature.Actions.ProcessInput(input);
            fsmRef.Creature.CameraController.ProcessInput(input);

            fsmRef.Creature.Input.ResetInput();
        }

        protected override void OnRender()
        {
            float deltaTime = Time.deltaTime;

            fsmRef.Creature.Movement.OnRender(deltaTime);
            fsmRef.Creature.Actions.OnRender();
        }
    }
}