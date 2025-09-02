using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterDataDefinition : ScriptableObject
    {
        // Config
        protected const int DEFINITION_BITS = 7;          // 0–127
        protected const int DEFINITION_SHIFT = 0;
        protected const byte DEFINITION_MASK = (1 << DEFINITION_BITS) - 1;

        protected const int SPAWN_TYPE_BITS = 3;          // 0–7
        protected const int SPAWN_TYPE_SHIFT = 0;
        protected const byte SPAWN_TYPE_MASK = (1 << SPAWN_TYPE_BITS) - 1;

        // Condition (byte)
        protected const int NPC_STATE_BITS = 4;           // 0–15
        protected const int ANIMATION_INDEX_BITS = 2;     // 0–3
        protected const int ATTITUDE_BITS = 2;              // 0–3

        protected const int NPC_STATE_SHIFT = 0;
        protected const int ATTITUDE_SHIFT = NPC_STATE_SHIFT + NPC_STATE_BITS;
        protected const int ANIMATION_INDEX_SHIFT = ATTITUDE_SHIFT + ATTITUDE_BITS;

        protected const byte NPC_STATE_MASK = (1 << NPC_STATE_BITS) - 1;
        protected const byte ATTITUDE_MASK = (1 << ATTITUDE_BITS) - 1;
        protected const byte ANIMATION_INDEX_MASK = (1 << ANIMATION_INDEX_BITS) - 1;

        public virtual void InitializeData(ref FNonPlayerCharacterData npcData, 
            NonPlayerCharacterDefinition definition,
            ENPCSpawnType spawnType,
            ETeamID teamID,
            EAttitude attitude,
            bool isInvasionNPC = false)
        {
            // Initialize Configuration
            npcData.Configuration = 0;
            SetDefinitionID(definition.TableID, ref npcData);

            // Initialize Condition
            npcData.Condition = 0;
            SetState(ENPCState.Idle, ref npcData);
            SetAttitude(EAttitude.Neutral, ref npcData);
            SetAnimationIndex(0, ref npcData);
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

        // DefinitionID
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
            return ETeamID.PlayerTeam;
        }

        public virtual void SetTeamID(ETeamID teamID, ref FNonPlayerCharacterData npcData)
        {
        }

        // NPCState
        public ENPCState GetState(ref FNonPlayerCharacterData npcData)
        {
            return (ENPCState)((npcData.Condition >> NPC_STATE_SHIFT) & NPC_STATE_MASK);
        }

        public void SetState(ENPCState newState, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            int stateValue = Mathf.Clamp((int)newState, 0, NPC_STATE_MASK);
            condition = (byte)((condition & ~(NPC_STATE_MASK << NPC_STATE_SHIFT)) | (stateValue << NPC_STATE_SHIFT));
            npcData.Condition = condition;
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
            int stateValue = Mathf.Clamp(animationState, 0, ANIMATION_INDEX_MASK);
            condition = (byte)((condition & ~(ANIMATION_INDEX_MASK << ANIMATION_INDEX_SHIFT)) | (stateValue << ANIMATION_INDEX_SHIFT));
            npcData.Condition = condition;
        }

        // Status
        public EAttitude GetAttitude(ref FNonPlayerCharacterData npcData)
        {
            return (EAttitude)((npcData.Condition >> ATTITUDE_SHIFT) & ATTITUDE_MASK);
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
    }
}
