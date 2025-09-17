using UnityEngine;

// A warrior is a player-summoned character.

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(menuName = "LichLord/NonPlayerCharacters/WarriorDataDefinition")]
    public class WarriorDataDefinition : NonPlayerCharacterDataDefinition
    {
        [SerializeField]
        private int _maxLifetimeProgress = 7;
        public int MaxLifetimeProgress => _maxLifetimeProgress;

        [SerializeField]
        private int _maxLifetimeTicks = 1440;
        public int MaximumLifetimeTicks => _maxLifetimeTicks;

        // Config 
        private const int PLAYER_FOLLOW_BITS = 4;             // 0–15
        private const int PLAYER_FOLLOW_SHIFT = DIALOG_INDEX_SHIFT + DIALOG_INDEX_BITS;
        private const ushort PLAYER_FOLLOW_MASK = (1 << PLAYER_FOLLOW_BITS) - 1;

        private const int FORMATION_ID_BITS = 4;             // 0–15
        private const int FORMATION_ID_SHIFT = PLAYER_FOLLOW_SHIFT + PLAYER_FOLLOW_BITS;
        private const ushort FORMATION_ID_MASK = (1 << FORMATION_ID_BITS) - 1;

        private const int FORMATION_INDEX_BITS = 4;             // 0–15
        private const int FORMATION_INDEX_SHIFT = FORMATION_ID_SHIFT + FORMATION_ID_BITS;
        private const ushort FORMATION_INDEX_MASK = (1 << FORMATION_INDEX_BITS) - 1;

        // Events
        private const int HEALTH_BITS = 12;             // 0–4095
        private const int HEALTH_SHIFT = 0;
        private const ushort HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        private const int LIFETIME_PROGRESS_BITS = 3;             // 0–7
        private const int LIFETIME_PROGRESS_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        private const ushort LIFETIME_PROGRESS_MASK = (1 << LIFETIME_PROGRESS_BITS) - 1;

        public override void InitializeData(ref FNonPlayerCharacterData npcData, 
            NonPlayerCharacterDefinition definition, 
            ENPCSpawnType spawnType,
            ETeamID teamID,
            EAttitude attitude)
        {
            base.InitializeData(ref npcData, definition, spawnType, teamID, attitude);

            // Initialize Events
            npcData.Events = 0;
            SetHealth(definition.MaxHealth, ref npcData);
            SetLifetimeProgress(0, ref npcData);
        }

        // Formation
        public int GetFormationID(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> FORMATION_ID_SHIFT) & FORMATION_ID_MASK;
        }

        public void SetFormationID(int formationId, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            formationId = Mathf.Clamp(formationId, 0, FORMATION_ID_MASK);
            config = (config & ~(FORMATION_ID_MASK << FORMATION_ID_SHIFT)) | (formationId << FORMATION_ID_SHIFT);
            npcData.Configuration = config;
        }

        public int GetFormationIndex(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> FORMATION_INDEX_SHIFT) & FORMATION_INDEX_MASK;
        }

        public void SetFormationIndex(int formationIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            formationIndex = Mathf.Clamp(formationIndex, 0, FORMATION_INDEX_MASK);
            config = (config & ~(FORMATION_INDEX_MASK << FORMATION_INDEX_SHIFT)) | (formationIndex << FORMATION_INDEX_SHIFT);
            npcData.Configuration = config;
        }

        // Player
        public int GetPlayerFollowIndex(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> PLAYER_FOLLOW_SHIFT) & PLAYER_FOLLOW_MASK;
        }

        public void SetPlayerFollowIndex(int playerIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            playerIndex = Mathf.Clamp(playerIndex, 0, PLAYER_FOLLOW_MASK);
            config = (config & ~(PLAYER_FOLLOW_MASK << PLAYER_FOLLOW_SHIFT)) | (playerIndex << PLAYER_FOLLOW_SHIFT);
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

        // Lifetime Progress
        public int GetLifetimeProgress(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Events >> LIFETIME_PROGRESS_SHIFT) & LIFETIME_PROGRESS_MASK;
        }

        public void SetLifetimeProgress(int newProgress, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            newProgress = Mathf.Clamp(newProgress, 0, LIFETIME_PROGRESS_MASK);
            events = (ushort)((events & ~(LIFETIME_PROGRESS_MASK << LIFETIME_PROGRESS_SHIFT)) | (newProgress << LIFETIME_PROGRESS_SHIFT));
            npcData.Events = events;
        }

        // Handle damage application
        public override void ApplyDamage(
            ref FNonPlayerCharacterData npcData,
            int damage, 
            int hitReactIndex)
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
