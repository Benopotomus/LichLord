using Fusion.Addons.FSM;
using UnityEngine;

namespace LichLord
{
    [Tooltip("The Idle state of the player.")]
    public class IdleState : CharacterStateBase
    {
        protected override void OnEnterStateRender()
        {
            //anim.CrossFadeInFixedTime(animState, 4f / 60f);
        }

        protected override void OnFixedUpdate()
        {
            FGameplayInput input = fsmRef.PC.Input.CurrentInput;

            fsmRef.PC.Movement.OnFixedUpdate(ref input);
            fsmRef.PC.CameraController.OnFixedUpdate(ref input);

            // Process activations
            fsmRef.PC.Maneuvers.ProcessInput(ref input);
            // Process timing
            fsmRef.PC.Maneuvers.OnFixedUpdate();

            CheckBuildMode(ref input);

            fsmRef.PC.Input.ResetInput();
        }

        protected override void OnRender()
        {
            float deltaTime = Time.deltaTime;

            fsmRef.PC.Movement.OnRender(deltaTime);
            fsmRef.PC.Maneuvers.OnRender();
        }

    }
}