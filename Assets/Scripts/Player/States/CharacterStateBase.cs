using Fusion.Addons.FSM;
using UnityEngine;

namespace LichLord
{
    /// <summary>
    /// Base class for different player states.
    /// </summary>
    public abstract class CharacterStateBase : StateBehaviour
    {
        [Tooltip("Reference to the Player's FSM; used for changing states and other FSM-related actions.")]
        public PlayerCharacterFSM fsmRef;

        protected override void OnFixedUpdate()
        {
            FGameplayInput input = fsmRef.PC.Input.CurrentInput;
        }

        protected virtual void CheckModeChange(ref FGameplayInput input)
        {
            if (input.BuildMode)
            {
                if (fsmRef.StateMachine.ActiveState is BuildModeState)
                    MoveToIdle();
                else
                    MoveToBuildMode();
                    
                return;
            }

            if (input.SummonMode)
            {
                if (fsmRef.StateMachine.ActiveState is SummonModeState)
                    MoveToIdle();
                else
                    MoveToSummonMode();

                return;
            }
        }

        protected override void OnRender()
        {
            

        }

        public void MoveToInteract()
        {
            fsmRef.StateMachine.TryActivateState<InteractingState>();
        }

        public void MoveToIdle()
        {
            fsmRef.StateMachine.TryActivateState<IdleState>();
        }

        public void MoveToBuildMode()
        {
            fsmRef.StateMachine.TryActivateState<BuildModeState>();
        }

        public void MoveToSummonMode()
        {
            fsmRef.StateMachine.TryActivateState<SummonModeState>();
        }
    }
}
