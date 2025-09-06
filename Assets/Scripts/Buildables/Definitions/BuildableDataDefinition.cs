using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableDataDefinition : ScriptableObject
    {
        [SerializeField]
        protected EBuildableState _startingState = EBuildableState.Idle;
        public EBuildableState StartingState => _startingState;

        protected const int STATE_BITS = 2;           // 0-3

        protected const int STATE_SHIFT = 0;

        protected const int STATE_MASK = (1 << STATE_BITS) - 1;

        public virtual void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {

        }

        public bool IsActive(FBuildableData propData)
        {
            return GetState(ref propData) != EBuildableState.Inactive;
        }

        // State
        public EBuildableState GetState(ref FBuildableData buildableData)
        {
            return (EBuildableState)((buildableData.StateData >> STATE_SHIFT) & STATE_MASK);
        }

        public void SetState(EBuildableState state, ref FBuildableData propData)
        {
            int stateData = propData.StateData;
            int stateValue = Mathf.Clamp((int)state, 0, STATE_MASK);
            stateData = (stateData & ~(STATE_MASK << STATE_SHIFT)) | (stateValue << STATE_SHIFT);
            propData.StateData = stateData;
        }

        // Prioritize destroyed state
        public virtual EBuildableState TryAssignState(ref FBuildableData propData, EBuildableState newState)
        {
            EBuildableState currentState = GetState(ref propData);

            switch (newState)
            {
                case EBuildableState.Inactive:
                    SetState(newState, ref propData);
                    return newState;
                case EBuildableState.HitReact:
                    switch (currentState)
                    {
                        case EBuildableState.Destroyed:
                        case EBuildableState.Inactive:

                            SetState(currentState, ref propData);
                            return currentState;
                    }
                    break;
            }

            SetState(currentState, ref propData);
            return newState;
        }
    }

    public enum EBuildableState : byte
    {
        Inactive,    // Not in the world
        Idle,        // Default active state
        HitReact,     // After taking damage
        Destroyed,   // Health <= 0
    }
}