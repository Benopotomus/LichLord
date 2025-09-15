using UnityEngine;

// A warrior is a player-summoned character.

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(menuName = "LichLord/NonPlayerCharacters/WarriorDataDefinition")]
    public class WarriorDataDefinition : NonPlayerCharacterDataDefinition
    {
        [Header("Formation Offsets")]
        [SerializeField] private Vector3[] _formationOffsets = new Vector3[16];

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

        public Vector3 GetFormationOffset(ref FNonPlayerCharacterData npcData)
        {
            int index = GetFormationIndex(ref npcData);
            if (index < 0 || index >= _formationOffsets.Length)
                return Vector3.zero;

            return _formationOffsets[index];
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
