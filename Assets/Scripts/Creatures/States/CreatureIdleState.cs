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
            fsmRef.Creature.Movement.OnRender();
            //fsmRef.Creature.Actions.OnRender();
        }
    }
}