using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(menuName = "LichLord/NonPlayerCharacters/WorkerDataDefinition")]
    public class WorkerDataDefinition : NonPlayerCharacterDataDefinition
    {
        // Config (7 from base)
        private const int TEAM_BITS = 3;                // 0–7
        private const int TEAM_SHIFT = DEFINITION_SHIFT + DEFINITION_BITS;
        private const byte TEAM_MASK = (1 << TEAM_BITS) - 1;

        private const int WORKER_INDEX_BITS = 6;                // 0–63
        private const int WORKER_INDEX_SHIFT = TEAM_SHIFT + TEAM_BITS;
        private const byte WORKER_INDEX_MASK = (1 << WORKER_INDEX_BITS) - 1;

        // Events
        private const int HEALTH_BITS = 8;             // 0–255
        private const int HEALTH_SHIFT = 0;
        private const ushort HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        private const int CARRIED_CURRENCY_TYPE_BITS = 4;
        private const int CARRIED_CURRENCY_TYPE_SHIFT = HEALTH_SHIFT + HEALTH_MASK;
        private const ushort CARRIED_CURRENCY_TYPE_MASK = (1 << CARRIED_CURRENCY_TYPE_BITS) - 1;

        private const int CARRIED_CURRENCY_STACKS_BITS = 4;
        private const int CARRIED_CURRENCY_STACKS_SHIFT = CARRIED_CURRENCY_TYPE_SHIFT + CARRIED_CURRENCY_STACKS_BITS;
        private const ushort CARRIED_CURRENCY_STACKS_MASK = (1 << CARRIED_CURRENCY_STACKS_BITS) - 1;

        public override void InitializeData(ref FNonPlayerCharacterData npcData, 
            NonPlayerCharacterDefinition definition, 
            ETeamID teamID, 
            bool isInvasionNPC = false)
        {
            base.InitializeData(ref npcData, definition, teamID, isInvasionNPC);

            // Initialize Config
            SetTeamID(teamID, ref npcData);

            // Get free worker index

            

            // Initialize Events
            npcData.Events = 0;
            SetHealth(definition.MaxHealth, ref npcData);
        }

        // TeamID
        public override ETeamID GetTeamID(ref FNonPlayerCharacterData npcData)
        {
            return (ETeamID)((npcData.Configuration >> TEAM_SHIFT) & TEAM_MASK);
        }

        public override void SetTeamID(ETeamID teamID, ref FNonPlayerCharacterData npcData)
        {
            ushort config = npcData.Configuration;
            int teamValue = Mathf.Clamp((int)teamID, 0, TEAM_MASK);
            config = (ushort)((config & ~(TEAM_MASK << TEAM_SHIFT)) | (teamValue << TEAM_SHIFT));
            npcData.Configuration = config;
        }

        // Worker Index
        public int GetWorkerIndex(ref FNonPlayerCharacterData npcData)
        {
            return (int)((npcData.Configuration >> WORKER_INDEX_SHIFT) & WORKER_INDEX_MASK);
        }

        public void SetWorkerIndex(int workerIndex, ref FNonPlayerCharacterData npcData)
        {
            ushort config = npcData.Configuration;
            int indexValue = Mathf.Clamp((int)workerIndex, 0, WORKER_INDEX_MASK);
            config = (ushort)((config & ~(WORKER_INDEX_MASK << WORKER_INDEX_SHIFT)) | (indexValue << WORKER_INDEX_SHIFT));
            npcData.Configuration = config;
        }

        // Health
        public int GetHealth(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Events >> HEALTH_SHIFT) & HEALTH_MASK;
        }

        public void SetHealth(int newHealth, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            newHealth = Mathf.Clamp(newHealth, 0, HEALTH_MASK);
            events = (ushort)((events & ~(HEALTH_MASK << HEALTH_SHIFT)) | (newHealth << HEALTH_SHIFT));
            npcData.Events = events;
        }

        // Handle damage application
        public override void ApplyDamage(ref FNonPlayerCharacterData npcData,
            int damage, int hitReactIndex)
        {
            int currentHealth = GetHealth(ref npcData);
            SetHealth(currentHealth - damage, ref npcData);

            if (GetHealth(ref npcData) == 0)
            {
                SetState(TryAssignState(ref npcData, ENPCState.Dead), ref npcData);
            }
            else
            {
                SetState(TryAssignState(ref npcData, ENPCState.HitReact), ref npcData);
                SetAnimationIndex(hitReactIndex, ref npcData);
            }
        }
    }
}
