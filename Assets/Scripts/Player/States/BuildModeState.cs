using UnityEngine;

namespace LichLord
{
    [Tooltip("The Build Mode state of the player.")]
    public class BuildModeState : CharacterStateBase
    {
        protected override void OnEnterStateRender()
        {
        }

        protected override void OnFixedUpdate()
        {
            float deltaTime = Runner.DeltaTime;
            int tick = Runner.Tick;

            FGameplayInput input = fsmRef.PC.Input.CurrentInput;

            // Authority
            fsmRef.PC.CameraController.ProcessInput(ref input);
            fsmRef.PC.Movement.ProcessInput(ref input, deltaTime);
            fsmRef.PC.Movement.UpdateLookRotation(deltaTime, 20f);

            fsmRef.PC.Builder.ProcessInput(ref input);

            // Call this for the ending of maneuvers
            fsmRef.PC.Movement.WritePosition();
            fsmRef.PC.Maneuvers.OnFixedUpdate();

            fsmRef.PC.Interactor.RefreshInteractables();
            fsmRef.PC.Interactor.ProcessInput(ref input);

            CheckBuildModeToggle(ref input);

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
            fsmRef.PC.Aim.OnRender(deltaTime);

            // Remote Only
            fsmRef.PC.Movement.UpdateRemotePosition(deltaTime);
            fsmRef.PC.Interactor.OnRender(deltaTime, localRenderTime, tick);
        }

        protected override void OnExitState()
        {
            base.OnExitState();
            fsmRef.PC.Builder.SetGhostVisibility(false);
        }
    }
}