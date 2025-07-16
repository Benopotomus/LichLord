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
            float deltaTime = Runner.DeltaTime;

            FGameplayInput input = fsmRef.PC.Input.CurrentInput;

            fsmRef.PC.Movement.ProcessInput(ref input, deltaTime);
            fsmRef.PC.Movement.OnFixedUpdateNetwork();
            fsmRef.PC.CameraController.ProcessInput(ref input);

            // Process input
            fsmRef.PC.Maneuvers.ProcessInput(ref input);
            // Process timing
            fsmRef.PC.Maneuvers.OnFixedUpdate();

            fsmRef.PC.Interactor.ProcessInput(ref input);
            fsmRef.PC.Interactor.OnFixedUpdate();

            CheckBuildMode(ref input);

            fsmRef.PC.Input.ResetInput();
        }

        protected override void OnRender()
        {
            base.OnRender();

            float deltaTime = Time.deltaTime;
            float localRenderTime = Runner.LocalRenderTime;
            int tick = Runner.Tick;

            // Both
            fsmRef.PC.Movement.OnRender(deltaTime);

            // Remote Only
            fsmRef.PC.Movement.UpdateRemotePosition(deltaTime);

            fsmRef.PC.Maneuvers.OnRender();
            fsmRef.PC.Interactor.OnRender(deltaTime, localRenderTime, tick);
        }

    }
}