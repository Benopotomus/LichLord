using Fusion;
using Fusion.Addons.FSM;
using System.Collections.Generic;
using UnityEngine;
namespace LichLord
{
    [Tooltip("The FSM that controls the player's states.")]
    [RequireComponent(typeof(StateMachineController))]
    public class PlayerCharacterFSM : NetworkBehaviour, IStateMachineOwner
    {
        private StateMachine<StateBehaviour> stateMachine;

        public StateMachine<StateBehaviour> StateMachine => stateMachine;

        [Tooltip("Reference to the Player controlled by this FSM.")]
        public PlayerCharacter PC;

        public void CollectStateMachines(List<IStateMachine> stateMachines)
        {
            var states = GetComponents<StateBehaviour>();
            stateMachine = new StateMachine<StateBehaviour>("Creature FSM", states);
            stateMachines.Add(stateMachine);
        }

    }
}