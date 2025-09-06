using UnityEditor;
using UnityEngine;

namespace LichLord.Props
{
    public class PropDataDefinition : ScriptableObject
    {
        [SerializeField]
        protected EPropState _startingState = EPropState.Idle;
        public EPropState StartingState => _startingState;

        // Bit size constants (matching PropDataDefinition)
        protected const int STATE_BITS = 4;           // 0-15

        protected const int STATE_SHIFT = 0;

        protected const int STATE_MASK = (1 << STATE_BITS) - 1;

        public virtual void InitializeData(ref FPropData propData, PropDefinition definition)
        {

        }

        public bool IsActive(FPropData propData)
        {
            return GetState(ref propData) != EPropState.Inactive;
        }

        // State
        public EPropState GetState(ref FPropData propData)
        {
            return (EPropState)((propData.StateData >> STATE_SHIFT) & STATE_MASK);
        }

        public void SetState(EPropState state, ref FPropData propData)
        {
            int stateData = propData.StateData;
            int stateValue = Mathf.Clamp((int)state, 0, STATE_MASK);
            stateData = (stateData & ~(STATE_MASK << STATE_SHIFT)) | (stateValue << STATE_SHIFT);
            propData.StateData = (ushort)stateData;
        }

        // Prioritize destroyed state
        public virtual EPropState TryAssignState(ref FPropData propData, EPropState newState)
        {
            EPropState currentState = GetState(ref propData);

            switch (newState)
            {
                case EPropState.Inactive:
                    SetState(newState, ref propData);
                    return newState;
                case EPropState.HitReact:
                    switch (currentState)
                    {
                        case EPropState.Destroyed:
                        case EPropState.Inactive:

                            SetState(currentState, ref propData);
                            return currentState;
                    }
                    break;
            }

            SetState(currentState, ref propData);
            return newState;
        }
    }

    public enum EPropState : byte
    {
        Inactive,    // Not in the world
        Idle,        // Default active state
        HitReact,     // After taking damage
        Destroyed,   // Health <= 0
    }

    public enum EPropStatus : byte
    {
        Neutral,     // Default state
        Alerted,     // Aware of interaction
        Engaged,     // Interacting with something
        Disabled,    // Temporarily unusable
    }
}