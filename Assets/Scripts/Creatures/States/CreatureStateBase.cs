using Fusion.Addons.FSM;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

namespace LichLord
{
    /// <summary>
    /// Base class for different player states.
    /// </summary>
    public abstract class CreatureStateBase : StateBehaviour
    {
        [Tooltip("Reference to the Player's FSM; used for changing states and other FSM-related actions.")]
        public CreatureFSM fsmRef;

        [Tooltip("Reference to the enemy's Animator.")]
        public Animator anim;

        [Tooltip("Name of the AnimatorState the enemy transitions to when entering this state.")]
        public string animState;

        [Tooltip("The length of the transition when playing this state's animation.")]
        public float animTransitionLength = 4f / 60f;
    }
}
