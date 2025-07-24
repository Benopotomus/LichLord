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
            int tick = Runner.Tick;

            FGameplayInput input = fsmRef.PC.Input.CurrentInput;

            // Authority
            fsmRef.PC.CameraController.ProcessInput(ref input);
            fsmRef.PC.Movement.ProcessInput(ref input, deltaTime);
            fsmRef.PC.Movement.UpdateLookRotation(deltaTime);
            fsmRef.PC.Maneuvers.ProcessInput(ref input);

            fsmRef.PC.Movement.WritePosition();
            fsmRef.PC.Maneuvers.OnFixedUpdate();

            fsmRef.PC.Interactor.RefreshInteractables();
            fsmRef.PC.Interactor.ProcessInput(ref input);

            //CheckBuildMode(ref input);

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
            fsmRef.PC.Interactor.OnRender(deltaTime, localRenderTime, tick);


            fsmRef.PC.Maneuvers.OnRender();
           
        }
    }
}