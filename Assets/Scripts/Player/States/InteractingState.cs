using UnityEngine;

namespace LichLord
{
    [Tooltip("The Interacting state of the player.")]
    public class InteractingState : CharacterStateBase
    {
        protected override void OnEnterStateRender()
        {
            //anim.CrossFadeInFixedTime(animState, 4f / 60f);
        }

        protected override void OnFixedUpdate()
        {
            FGameplayInput input = fsmRef.PC.Input.CurrentInput;

            fsmRef.PC.CameraController.ProcessInput(ref input);

            fsmRef.PC.Interactor.ProcessInput(ref input);
            fsmRef.PC.Interactor.OnFixedUpdate();

            fsmRef.PC.Input.ResetInput();
        }

        protected override void OnRender()
        {
            float deltaTime = Time.deltaTime;
            float localRenderTime = Runner.LocalRenderTime;
            int tick = Runner.Tick;

            //fsmRef.PC.Movement.OnRender(deltaTime);
            //fsmRef.PC.Maneuvers.OnRender();
            fsmRef.PC.Interactor.OnRender(deltaTime, localRenderTime, tick);
        }

    }
}