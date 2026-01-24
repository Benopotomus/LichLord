using UnityEngine;

namespace LichLord
{
    [Tooltip("The command state of the player.")]
    public class CommandModeState : CharacterStateBase
    {
        protected override void OnEnterStateRender()
        {
            //anim.CrossFadeInFixedTime(animState, 4f / 60f);
        }

        protected override void OnEnterState()
        {
            base.OnEnterState();
            fsmRef.PC.Maneuvers.OnEnterState(EManeuverList.Commands);
        }

        protected override void OnExitState()
        {
            base.OnExitState();
            fsmRef.PC.Maneuvers.OnExitState();
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
            fsmRef.PC.Maneuvers.ProcessInput(ref input);
            fsmRef.PC.Maneuvers.UpdateMoveSpeed(deltaTime);

            fsmRef.PC.Movement.WritePosition();
            fsmRef.PC.Maneuvers.OnFixedUpdate();

            fsmRef.PC.Interactor.RefreshInteractables();
            fsmRef.PC.Interactor.ProcessInput(ref input);

            CheckModeChange(ref input);

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
            fsmRef.PC.AnimationController.UpdateAnimationForWeapon();

            fsmRef.PC.Aim.OnRender(deltaTime);
            fsmRef.PC.Weapons.OnRender(deltaTime);

            // Remote Only
            fsmRef.PC.Movement.UpdateRemotePosition(deltaTime);
            fsmRef.PC.Interactor.OnRender(deltaTime, localRenderTime, tick);
        }
    }
}