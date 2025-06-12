using UnityEditor;
using UnityEngine;

namespace LichLord.Props
{
    [CreateAssetMenu(fileName = "PropDataDefinition", menuName = "LichLord/Props/PropDataDefinition")]
    public class PropDataDefinition : ScriptableObject
    {
        [SerializeField]
        private int _maxHealth = 100;
        public int MaxHealth => _maxHealth;

        [SerializeField]
        private EPropState _startingState = EPropState.Idle;
        public EPropState StartingState => _startingState;

        // Bit size constants (matching PropDataDefinition)
        private const int STATE_BITS = 4;           // 0-15
        private const int STATUS_BITS = 4;          // 0-15
        private const int HEALTH_BITS = 12;         // 0-4095

        // Bit shifts and masks for StateData (int)
        private const int STATE_SHIFT = 0;
        private const int STATUS_SHIFT = STATE_SHIFT + STATE_BITS;
        private const int HEALTH_SHIFT = STATUS_SHIFT + STATUS_BITS;
        private const int STATE_MASK = (1 << STATE_BITS) - 1;
        private const int STATUS_MASK = (1 << STATUS_BITS) - 1;
        private const int HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        public void InitializeData(ref FPropData propData, PropDefinition definition)
        {
            // Initialize fields
            propData.DefinitionID = definition.TableID; // Assuming definition has an ID
            propData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref propData);
            SetStatus(EPropStatus.Neutral, ref propData);
            SetHealth(MaxHealth, ref propData); // Default health, adjust as needed
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
            propData.StateData = stateData;
        }

        // Status
        public EPropStatus GetStatus(ref FPropData propData)
        {
            return (EPropStatus)((propData.StateData >> STATUS_SHIFT) & STATUS_MASK);
        }

        public void SetStatus(EPropStatus status, ref FPropData propData)
        {
            int stateData = propData.StateData;
            int statusValue = Mathf.Clamp((int)status, 0, STATUS_MASK);
            stateData = (stateData & ~(STATUS_MASK << STATUS_SHIFT)) | (statusValue << STATUS_SHIFT);
            propData.StateData = stateData;
        }

        // Health
        public int GetHealth(ref FPropData propData)
        {
            return (propData.StateData >> HEALTH_SHIFT) & HEALTH_MASK;
        }

        public void SetHealth(int health, ref FPropData propData)
        {
            int stateData = propData.StateData;
            health = Mathf.Clamp(health, 0, HEALTH_MASK);
            stateData = (stateData & ~(HEALTH_MASK << HEALTH_SHIFT)) | (health << HEALTH_SHIFT);
            propData.StateData = stateData;
        }

        public bool IsActive(FPropData propData)
        {
            return GetState(ref propData) != EPropState.Inactive;
        }

        // Handle damage application
        public void ApplyDamage(ref FPropData propData, int damage)
        {
            int currentHealth = GetHealth(ref propData);
            SetHealth(currentHealth - damage, ref propData);

            Debug.Log($"Apply Damage " + propData.GUID + ", Health: " + GetHealth(ref propData));

            if (GetHealth(ref propData) <= 0)
            {
                SetState(TryAssignState(ref propData, EPropState.Destroyed), ref propData);
            }
            else
            {
                SetState(TryAssignState(ref propData, EPropState.HitReact), ref propData);
            }
        }

        // Prioritize destroyed state
        public EPropState TryAssignState(ref FPropData propData, EPropState newState)
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
        Action_1,      // Performing an action
    }

    public enum EPropStatus : byte
    {
        Neutral,     // Default state
        Alerted,     // Aware of interaction
        Engaged,     // Interacting with something
        Disabled,    // Temporarily unusable
    }
}