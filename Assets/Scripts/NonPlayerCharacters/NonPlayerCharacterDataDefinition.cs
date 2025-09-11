using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterDataDefinition : ScriptableObject
    {
        private const int INVALID_DIALOG_INDEX = DIALOG_INDEX_MASK; // Reserve max value as invalid

        // Config
        protected const int DEFINITION_BITS = 8;          // 0–255
        protected const int DEFINITION_SHIFT = 0;
        protected const byte DEFINITION_MASK = (1 << DEFINITION_BITS) - 1;

        protected const int SPAWN_TYPE_BITS = 2;          // 0–3
        protected const int SPAWN_TYPE_SHIFT = DEFINITION_SHIFT + DEFINITION_BITS;
        protected const byte SPAWN_TYPE_MASK = (1 << SPAWN_TYPE_BITS) - 1;

        protected const int TEAM_BITS = 3;                // 0–7
        protected const int TEAM_SHIFT = SPAWN_TYPE_SHIFT + SPAWN_TYPE_BITS;
        protected const byte TEAM_MASK = (1 << TEAM_BITS) - 1;

        protected const int DIALOG_INDEX_BITS = 5;        // 0-31
        protected const int DIALOG_INDEX_SHIFT = TEAM_SHIFT + TEAM_BITS;
        protected const ushort DIALOG_INDEX_MASK = (1 << DIALOG_INDEX_BITS) - 1;
        // 18 total

        // Condition (byte)
        protected const int NPC_STATE_BITS = 4;           // 0–15
        protected const int ANIMATION_INDEX_BITS = 2;     // 0–3
        protected const int ATTITUDE_BITS = 2;              // 0–3

        protected const int NPC_STATE_SHIFT = 0;
        protected const int ANIMATION_INDEX_SHIFT = NPC_STATE_SHIFT + NPC_STATE_BITS;
        protected const int ATTITUDE_SHIFT = ANIMATION_INDEX_SHIFT + ANIMATION_INDEX_BITS;

        protected const byte NPC_STATE_MASK = (1 << NPC_STATE_BITS) - 1;
        protected const byte ANIMATION_INDEX_MASK = (1 << ANIMATION_INDEX_BITS) - 1;
        protected const byte ATTITUDE_MASK = (1 << ATTITUDE_BITS) - 1;

        public virtual void InitializeData(ref FNonPlayerCharacterData npcData, 
            NonPlayerCharacterDefinition definition,
            ENPCSpawnType spawnType,
            ETeamID teamID,
            EAttitude attitude)
        {
            // Initialize Configuration
            npcData.Configuration = 0;
            SetDefinitionID(definition.TableID, ref npcData);
            SetSpawnType(spawnType, ref npcData);
            SetTeamID(teamID, ref npcData);
            SetDialogIndex(-1, ref npcData);

            // Initialize Condition
            npcData.Condition = 0;
            SetState(ENPCState.Idle, ref npcData);
            SetAnimationIndex(0, ref npcData);
            SetAttitude(attitude, ref npcData);
        }

        // DefinitionID
        public int GetDefinitionID(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> DEFINITION_SHIFT) & DEFINITION_MASK;
        }

        public void SetDefinitionID(int definitionIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            definitionIndex = Mathf.Clamp(definitionIndex, 0, DEFINITION_MASK);
            config = (config & ~(DEFINITION_MASK << DEFINITION_SHIFT)) | (definitionIndex << DEFINITION_SHIFT);
            npcData.Configuration = config;
        }

        // Spawn Type
        public ENPCSpawnType GetSpawnType(ref FNonPlayerCharacterData npcData)
        {
            return (ENPCSpawnType)((npcData.Configuration >> SPAWN_TYPE_SHIFT) & SPAWN_TYPE_MASK);
        }

        public void SetSpawnType(ENPCSpawnType newSpawnType, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int spawnType = Mathf.Clamp((int)newSpawnType, 0, SPAWN_TYPE_MASK);
            config = (config & ~(SPAWN_TYPE_MASK << SPAWN_TYPE_SHIFT)) | (spawnType << SPAWN_TYPE_SHIFT);
            npcData.Configuration = config;
        }

        // TeamID
        public virtual ETeamID GetTeamID(ref FNonPlayerCharacterData npcData)
        {
            return (ETeamID)((npcData.Configuration >> TEAM_SHIFT) & TEAM_MASK);
        }

        public virtual void SetTeamID(ETeamID teamID, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int teamValue = Mathf.Clamp((int)teamID, 0, TEAM_MASK);
            config = (ushort)((config & ~(TEAM_MASK << TEAM_SHIFT)) | (teamValue << TEAM_SHIFT));
            npcData.Configuration = config;
        }

        // Dialog
        public virtual int GetDialogIndex(ref FNonPlayerCharacterData npcData)
        {
            int index = (npcData.Configuration >> DIALOG_INDEX_SHIFT) & DIALOG_INDEX_MASK;
            if (index == INVALID_DIALOG_INDEX)
                return -1; // Use -1 as "no dialog"
            return index;
        }

        public virtual void SetDialogIndex(int newDialogIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int dialogIndex;

            if (newDialogIndex < 0)
                dialogIndex = INVALID_DIALOG_INDEX; // store invalid for "no dialog"
            else
                dialogIndex = Mathf.Clamp(newDialogIndex, 0, DIALOG_INDEX_MASK - 1);

            config = (config & ~(DIALOG_INDEX_MASK << DIALOG_INDEX_SHIFT)) | (dialogIndex << DIALOG_INDEX_SHIFT);
            npcData.Configuration = config;
        }

        public virtual bool HasDialog(ref FNonPlayerCharacterData npcData)
        {
            return (GetDialogIndex(ref npcData) > -1);
        }

        // NPCState
        public ENPCState GetState(ref FNonPlayerCharacterData npcData)
        {
            return (ENPCState)((npcData.Condition >> NPC_STATE_SHIFT) & NPC_STATE_MASK);
        }

        public void SetState(ENPCState newState, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            //Debug.Log($"Before SetState: Condition=0x{condition:X2}, Attitude={GetAttitude(ref npcData)}");
            int stateValue = Mathf.Clamp((int)newState, 0, NPC_STATE_MASK);
            condition = (byte)((condition & ~(NPC_STATE_MASK << NPC_STATE_SHIFT)) | (stateValue << NPC_STATE_SHIFT));
            npcData.Condition = condition;
            //Debug.Log($"After SetState: Condition=0x{condition:X2}, Attitude={GetAttitude(ref npcData)}");
        }

        public virtual ENPCState TryAssignState(ref FNonPlayerCharacterData npcData, ENPCState newState)
        {
            ENPCState currentState = GetState(ref npcData);

            switch (newState)
            {
                case ENPCState.Inactive:
                    return newState;
                case ENPCState.HitReact:
                    switch (currentState)
                    {
                        case ENPCState.Dead:
                        case ENPCState.Inactive:
                            return currentState;
                    }
                    break;
            }

            return newState;
        }

        // Animation
        public int GetAnimationIndex(ref FNonPlayerCharacterData npcData)
        {
            return ((npcData.Condition >> ANIMATION_INDEX_SHIFT) & ANIMATION_INDEX_MASK);
        }

        public void SetAnimationIndex(int animationState, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            //Debug.Log($"Before SetAnimationIndex: Condition=0x{condition:X2}, Attitude={GetAttitude(ref npcData)}");
            int stateValue = Mathf.Clamp(animationState, 0, ANIMATION_INDEX_MASK);
            condition = (byte)((condition & ~(ANIMATION_INDEX_MASK << ANIMATION_INDEX_SHIFT)) | (stateValue << ANIMATION_INDEX_SHIFT));
            npcData.Condition = condition;
            //Debug.Log($"After SetAnimationIndex: Condition=0x{condition:X2}, Attitude={GetAttitude(ref npcData)}");
        }

        // attitude
        public EAttitude GetAttitude(ref FNonPlayerCharacterData npcData)
        {
            int rawValue = (npcData.Condition >> ATTITUDE_SHIFT) & ATTITUDE_MASK;
            EAttitude attitude = (EAttitude)rawValue;
            //Debug.Log($"GetAttitude for NPC {npcData.DefinitionID}: Condition=0x{npcData.Condition:X2}, Raw Attitude Bits={rawValue}, Returned Attitude={attitude}");
            //Debug.Assert(rawValue >= 0 && rawValue <= ATTITUDE_MASK, $"Invalid Attitude bits: {rawValue} in Condition=0x{npcData.Condition:X2}");
            return attitude;
        }

        public void SetAttitude(EAttitude attitude, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            int statusValue = Mathf.Clamp((int)attitude, 0, ATTITUDE_MASK);
            condition = (byte)((condition & ~(ATTITUDE_MASK << ATTITUDE_SHIFT)) | (statusValue << ATTITUDE_SHIFT));
            npcData.Condition = condition;
        }

        // Handle damage application
        public virtual void ApplyDamage(
            ref FNonPlayerCharacterData npcData,
            int damage, 
            int hitReactIndex)
        {
          
        }

        // Health
        public virtual int GetHealth(ref FNonPlayerCharacterData npcData)
        {
            return -1;
        }

        public virtual void SetHealth(int newHealth, ref FNonPlayerCharacterData npcData)
        {

        }

        public void SetStateAndAnimation(ENPCState newState, int animationState, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            EAttitude beforeAttitude = GetAttitude(ref npcData);
            Debug.Log($"SetStateAndAnimation for NPC {npcData.GetHashCode()}: Before - Condition=0x{condition:X2}, State={GetState(ref npcData)}, AnimationIndex={GetAnimationIndex(ref npcData)}, Attitude={beforeAttitude}, NewState={newState}, NewAnimation={animationState}, Caller={new System.Diagnostics.StackTrace()}");

            // Update State (bits 0–3)
            int stateValue = Mathf.Clamp((int)newState, 0, NPC_STATE_MASK);
            condition = (byte)((condition & ~(NPC_STATE_MASK << NPC_STATE_SHIFT)) | (stateValue << NPC_STATE_SHIFT));

            // Update AnimationIndex (bits 6–7)
            int animValue = Mathf.Clamp(animationState, 0, ANIMATION_INDEX_MASK);
            condition = (byte)((condition & ~(ANIMATION_INDEX_MASK << ANIMATION_INDEX_SHIFT)) | (animValue << ANIMATION_INDEX_SHIFT));

            npcData.Condition = condition;

            EAttitude afterAttitude = GetAttitude(ref npcData);
            Debug.Log($"SetStateAndAnimation: After - Condition=0x{condition:X2}, State={GetState(ref npcData)}, AnimationIndex={GetAnimationIndex(ref npcData)}, Attitude={afterAttitude}");
            if (beforeAttitude != afterAttitude)
            {
                Debug.LogWarning($"Attitude changed unexpectedly from {beforeAttitude} to {afterAttitude} in SetStateAndAnimation!");
            }
        }
    }
}
