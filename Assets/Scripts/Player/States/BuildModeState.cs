using Fusion.Addons.FSM;
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
            FGameplayInput input = fsmRef.PC.Input.CurrentInput;


            //fsmRef.PC.Movement.Move(ref input);
            fsmRef.PC.CameraController.ProcessInput(ref input);
            fsmRef.PC.Builder.ProcessInput(ref input);

            // Call this for the ending of maneuvers
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

        protected override void OnExitState()
        {
            base.OnExitState();
            fsmRef.PC.Builder.SetGhostVisibility(false);
        }
    }
}