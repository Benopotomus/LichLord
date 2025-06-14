namespace LichLord.NonPlayerCharacters
{
    using System;
    using UnityEngine;

    public static class NonPlayerCharacterDataUtility
    {
        // Bit size constants
        private const int INDEX_BITS = 9;                // 0-511
        private const int DEFINITION_BITS = 5;           // 0-31
        private const int TEAM_BITS = 2;                // 0-3

        private const int NPC_STATE_BITS = 4;           // 0-15
        private const int STATUS_BITS = 2;              // 0-3
        private const int ANIMATION_INDEX_BITS = 2;      // 0-3

        private const int HEALTH_BITS = 12;             // 0-4095

        // Bit shifts and masks for Configuration (ushort)
        private const int INDEX_SHIFT = 0;
        private const int DEFINITION_SHIFT = INDEX_SHIFT + INDEX_BITS;
        private const int TEAM_SHIFT = DEFINITION_SHIFT + DEFINITION_BITS;
        private const ushort INDEX_MASK = (1 << INDEX_BITS) - 1;
        private const ushort DEFINITION_MASK = (1 << DEFINITION_BITS) - 1;
        private const ushort TEAM_MASK = (1 << TEAM_BITS) - 1;

        // Bit shifts and masks for Condition (byte)
        private const int NPC_STATE_SHIFT = 0;
        private const int STATUS_SHIFT = NPC_STATE_SHIFT + NPC_STATE_BITS;
        private const int ANIMATION_INDEX_SHIFT = STATUS_SHIFT + STATUS_BITS;
        private const byte NPC_STATE_MASK = (1 << NPC_STATE_BITS) - 1;
        private const byte STATUS_MASK = (1 << STATUS_BITS) - 1;
        private const byte ANIMATION_INDEX_MASK = (1 << ANIMATION_INDEX_BITS) - 1;

        // Bit shifts and masks for Events (ushort)
        private const int HEALTH_SHIFT = 0;
        private const ushort HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        public static void InitializeData(ref FNonPlayerCharacterData npcData, NonPlayerCharacterDefinition definition, int index, ETeamID teamID)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition), "NPC definition cannot be null.");

            // Initialize Configuration
            npcData.Configuration = 0;
            SetGUID(index, ref npcData);
            SetDefinitionID(definition.TableID, ref npcData);
            SetTeamID(teamID, ref npcData);

            // Initialize Events
            npcData.Events = 0;
            SetHealth(definition.MaxHealth, ref npcData);

            // Initialize Condition
            npcData.Condition = 0;
            SetNPCState(ENonPlayerState.Idle, ref npcData);
            SetStatus(ENPCStatus.Neutral, ref npcData);
            SetAnimationIndex(0, ref npcData);
        }

        // Index
        public static int GetGUID(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> INDEX_SHIFT) & INDEX_MASK;
        }

        public static void SetGUID(int index, ref FNonPlayerCharacterData npcData)
        {
            ushort config = npcData.Configuration;
            index = Mathf.Clamp(index, 0, INDEX_MASK);
            config = (ushort)((config & ~(INDEX_MASK << INDEX_SHIFT)) | (index << INDEX_SHIFT));
            npcData.Configuration = config;
        }

        // DefinitionID
        public static int GetDefinitionID(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> DEFINITION_SHIFT) & DEFINITION_MASK;
        }

        public static void SetDefinitionID(int definitionIndex, ref FNonPlayerCharacterData npcData)
        {
            ushort config = npcData.Configuration;
            definitionIndex = Mathf.Clamp(definitionIndex, 0, DEFINITION_MASK);
            config = (ushort)((config & ~(DEFINITION_MASK << DEFINITION_SHIFT)) | (definitionIndex << DEFINITION_SHIFT));
            npcData.Configuration = config;
        }

        // TeamID
        public static ETeamID GetTeamID(ref FNonPlayerCharacterData npcData)
        {
            return (ETeamID)((npcData.Configuration >> TEAM_SHIFT) & TEAM_MASK);
        }

        public static void SetTeamID(ETeamID teamID, ref FNonPlayerCharacterData npcData)
        {
            ushort config = npcData.Configuration;
            int teamValue = Mathf.Clamp((int)teamID, 0, TEAM_MASK);
            config = (ushort)((config & ~(TEAM_MASK << TEAM_SHIFT)) | (teamValue << TEAM_SHIFT));
            npcData.Configuration = config;
        }

        // Health
        public static int GetHealth(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Events >> HEALTH_SHIFT) & HEALTH_MASK;
        }

        public static void SetHealth(int newHealth, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            newHealth = Mathf.Clamp(newHealth, 0, HEALTH_MASK);
            events = (ushort)((events & ~(HEALTH_MASK << HEALTH_SHIFT)) | (newHealth << HEALTH_SHIFT));
            npcData.Events = events;
        }

        // Status
        public static ENPCStatus GetStatus(ref FNonPlayerCharacterData npcData)
        {
            return (ENPCStatus)((npcData.Condition >> STATUS_SHIFT) & STATUS_MASK);
        }

        public static void SetStatus(ENPCStatus status, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            int statusValue = Mathf.Clamp((int)status, 0, STATUS_MASK);
            condition = (byte)((condition & ~(STATUS_MASK << STATUS_SHIFT)) | (statusValue << STATUS_SHIFT));
            npcData.Condition = condition;
        }

        // NPCState
        public static ENonPlayerState GetNPCState(ref FNonPlayerCharacterData npcData)
        {
            return (ENonPlayerState)((npcData.Condition >> NPC_STATE_SHIFT) & NPC_STATE_MASK);
        }

        public static void SetNPCState(ENonPlayerState newState, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            int stateValue = Mathf.Clamp((int)newState, 0, NPC_STATE_MASK);
            condition = (byte)((condition & ~(NPC_STATE_MASK << NPC_STATE_SHIFT)) | (stateValue << NPC_STATE_SHIFT));
            npcData.Condition = condition;
        }

        // AnimationState
        public static int GetAnimationIndex(ref FNonPlayerCharacterData npcData)
        {
            return ((npcData.Condition >> ANIMATION_INDEX_SHIFT) & ANIMATION_INDEX_MASK);
        }

        public static void SetAnimationIndex(int animationState, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            int stateValue = Mathf.Clamp(animationState, 0, ANIMATION_INDEX_MASK);
            condition = (byte)((condition & ~(ANIMATION_INDEX_MASK << ANIMATION_INDEX_SHIFT)) | (stateValue << ANIMATION_INDEX_SHIFT));
            npcData.Condition = condition;
        }

        public static bool IsActive(FNonPlayerCharacterData npcData)
        {
            return GetNPCState(ref npcData) != ENonPlayerState.Inactive;
        }

        // Handle damage application by type
        public static void ApplyDamage(ref FNonPlayerCharacterData npcData,
            NonPlayerCharacterDefinition definition,
            int damage)
        {
            int currentHealth = GetHealth(ref npcData);
            SetHealth(currentHealth - damage, ref npcData);

            Debug.Log("Apply Damage Health " + GetHealth(ref npcData));
            // Determine what happens when health is reduced
            if (GetHealth(ref npcData) == 0)
            {
                SetNPCState(TryAssignState(ref npcData, ENonPlayerState.Dead), ref npcData);
            }
            else
            {
                SetNPCState(TryAssignState(ref npcData, ENonPlayerState.HitReact), ref npcData);
            }
        }

        // Prioritize the dead states over idle and hit react
        public static ENonPlayerState TryAssignState(ref FNonPlayerCharacterData npcData, ENonPlayerState newState)
        {
            ENonPlayerState currentState = GetNPCState(ref npcData);

            switch (newState)
            {
                case ENonPlayerState.Inactive:
                    return newState;
                case ENonPlayerState.HitReact:
                    switch (currentState)
                    {
                        case ENonPlayerState.Dead:
                        case ENonPlayerState.Inactive:
                            return currentState;
                    }
                    break;
            }

            return newState;
        }
    }

    // Existing enums (unchanged, included for completeness)
    public enum ENonPlayerState : byte
    {
        Inactive,    // Not in the world at all
        Idle,
        Dead,
        HitReact,
        Maneuver_1,
        Maneuver_2,
        Maneuver_3,
        Maneuver_4,
    }

    public enum ENPCStatus : byte
    {
        Neutral,        // Default state
        Alerted,        // Aware of potential threat
        Engaged,        // Actively in combat
        Stunned,        // Temporarily incapacitated
        Fleeing,        // Attempting to escape
        Patrolling,     // Following a patrol route
        Searching,      // Looking for target
        Disabled,       // Temporarily out of action
    }

}