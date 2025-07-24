using Fusion;
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
            float deltaTime = Runner.DeltaTime;
            int tick = Runner.Tick;

            FGameplayInput input = fsmRef.PC.Input.CurrentInput;
            fsmRef.PC.CameraController.ProcessInput(ref input);

            fsmRef.PC.Movement.WritePosition();
            fsmRef.PC.Maneuvers.OnFixedUpdate();

            fsmRef.PC.Interactor.RefreshInteractables();
            fsmRef.PC.Interactor.ProcessInput(ref input);
            fsmRef.PC.Interactor.OnFixedUpdateNetwork(tick, deltaTime);

            fsmRef.PC.Input.ResetInput();
        }

        protected override void OnRender()
        {
            float deltaTime = Time.deltaTime;
            float localRenderTime = Runner.LocalRenderTime;
            int tick = Runner.Tick;

            // Both
            fsmRef.PC.Movement.OnRender(deltaTime);
            fsmRef.PC.Interactor.OnRender(deltaTime, localRenderTime, tick);

            // Remote Only
            fsmRef.PC.Movement.UpdateRemotePosition(deltaTime);
        }

    }
}