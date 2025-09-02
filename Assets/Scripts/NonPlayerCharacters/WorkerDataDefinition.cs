using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(menuName = "LichLord/NonPlayerCharacters/WorkerDataDefinition")]
    public class WorkerDataDefinition : NonPlayerCharacterDataDefinition
    {
        // Config (7 from base)
        private const int WORKER_INDEX_BITS = 7;                // 0–127
        private const int WORKER_INDEX_SHIFT = DEFINITION_SHIFT + DEFINITION_BITS;
        private const byte WORKER_INDEX_MASK = (1 << WORKER_INDEX_BITS) - 1;

        // Events (packed into ushort)
        private const int HEALTH_BITS = 8;               // 0–255
        private const int HEALTH_SHIFT = 0;
        private const ushort HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        private const int CARRIED_CURRENCY_TYPE_BITS = 4; // 0–15
        private const int CARRIED_CURRENCY_TYPE_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        private const ushort CARRIED_CURRENCY_TYPE_MASK = (1 << CARRIED_CURRENCY_TYPE_BITS) - 1;

        private const int HARVEST_PROGRESS_BITS = 4; // 0–15
        private const int HARVEST_PROGRESS_SHIFT = CARRIED_CURRENCY_TYPE_SHIFT + CARRIED_CURRENCY_TYPE_BITS;
        private const ushort HARVEST_PROGRESS_MASK = (1 << HARVEST_PROGRESS_BITS) - 1;

        public override void InitializeData(ref FNonPlayerCharacterData npcData, 
            NonPlayerCharacterDefinition definition, 
            ENPCSpawnType spawnType,
            ETeamID teamID, 
            EAttitude attitude,
            bool isInvasionNPC = false)
        {
            base.InitializeData(ref npcData, definition, spawnType, teamID, attitude, isInvasionNPC);

            // Initialize Events
            npcData.Events = 0;
            SetHealth(definition.MaxHealth, ref npcData);
        }

        // TeamID
        public override ETeamID GetTeamID(ref FNonPlayerCharacterData npcData)
        {
            return ETeamID.PlayerTeam;
        }

        public override void SetTeamID(ETeamID teamID, ref FNonPlayerCharacterData npcData)
        {
        }

        // Worker Index
        public int GetWorkerIndex(ref FNonPlayerCharacterData npcData)
        {
            return (int)((npcData.Configuration >> WORKER_INDEX_SHIFT) & WORKER_INDEX_MASK);
        }

        public void SetWorkerIndex(int workerIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int indexValue = Mathf.Clamp((int)workerIndex, 0, WORKER_INDEX_MASK);
            config = ((config & ~(WORKER_INDEX_MASK << WORKER_INDEX_SHIFT)) | (indexValue << WORKER_INDEX_SHIFT));
            npcData.Configuration = config;
        }

        // Health
        public override int GetHealth(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Events >> HEALTH_SHIFT) & HEALTH_MASK;
        }

        public override void SetHealth(int newHealth, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            newHealth = Mathf.Clamp(newHealth, 0, HEALTH_MASK);
            events = (ushort)((events & ~(HEALTH_MASK << HEALTH_SHIFT)) | (newHealth << HEALTH_SHIFT));
            npcData.Events = events;
        }

        // Currency Type
        public ECurrencyType GetCurrencyType(ref FNonPlayerCharacterData npcData)
        {
            return (ECurrencyType)((npcData.Events >> CARRIED_CURRENCY_TYPE_SHIFT) & CARRIED_CURRENCY_TYPE_MASK);
        }

        public void SetCurrencyType(ECurrencyType newCurrencyType, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            int newTypeIndex = Mathf.Clamp((int)newCurrencyType, 0, CARRIED_CURRENCY_TYPE_MASK);
            events = (ushort)((events & ~(CARRIED_CURRENCY_TYPE_MASK << CARRIED_CURRENCY_TYPE_SHIFT)) | (newTypeIndex << CARRIED_CURRENCY_TYPE_SHIFT));
            npcData.Events = events;
        }

        // Harvest Progress
        public int GetHarvestProgress(ref FNonPlayerCharacterData npcData)
        {
            return (int)((npcData.Events >> HARVEST_PROGRESS_SHIFT) & HARVEST_PROGRESS_MASK);
        }

        public void SetHarvestProgress(int newCurrencyStacks, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            int newStacksCount = Mathf.Clamp((int)newCurrencyStacks, 0, HARVEST_PROGRESS_MASK);
            events = (ushort)((events & ~(HARVEST_PROGRESS_MASK << HARVEST_PROGRESS_SHIFT)) | (newStacksCount << HARVEST_PROGRESS_SHIFT));
            npcData.Events = events;
        }

        // Handle damage application
        public override void ApplyDamage(ref FNonPlayerCharacterData npcData,
            int damage, int hitReactIndex)
        {
            NonPlayerCharacterDefinition definition = npcData.Definition;

            int currentHealth = GetHealth(ref npcData);
            damage = Mathf.Max(damage - definition.DamageReduction, 0);
            damage = (int)((float)damage * (1.0f - definition.DamageResistance));

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
