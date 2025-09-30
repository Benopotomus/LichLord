using UnityEngine;

namespace LichLord
{
    [Tooltip("The summon state of the player.")]
    public class SummonModeState : CharacterStateBase
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
            fsmRef.PC.Movement.UpdateLookRotation(deltaTime, 20f);
            fsmRef.PC.Summoner.ProcessInput(ref input);
            //fsmRef.PC.Summoner.UpdateMoveSpeed(deltaTime);

            fsmRef.PC.Movement.WritePosition();
            fsmRef.PC.Summoner.OnFixedUpdate();

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