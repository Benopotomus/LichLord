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

        protected virtual void CheckBuildMode(ref FGameplayInput input)
        {

            if (fsmRef.StateMachine.ActiveState is IdleState)
            {
                if (input.BuildMode)
                {
                    MoveToBuildMode();
                    return;
                }
            }
            else if (fsmRef.StateMachine.ActiveState is BuildModeState)
            {
                if (input.BuildMode)
                {
                    MoveToIdle();
                    return;
                }
            }
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
    }
}
