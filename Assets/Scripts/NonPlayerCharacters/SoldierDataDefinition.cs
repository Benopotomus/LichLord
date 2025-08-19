using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(menuName = "LichLord/NonPlayerCharacters/SoldierDataDefinition")]
    public class SoldierDataDefinition : NonPlayerCharacterDataDefinition
    {
        // Config (7 from base)
        private const int TEAM_BITS = 3;                // 0–7
        private const int INVASION_NPC_BITS = 1;        // 0-1
       // private const int FORMATION_ID_BITS = 3;        // 0-7
       // private const int FORMATION_INDEX_BITS = 4;     // 0-15

        private const int TEAM_SHIFT = DEFINITION_SHIFT + DEFINITION_BITS;
        private const int INVASION_NPC_SHIFT = TEAM_SHIFT + TEAM_BITS;

        private const byte TEAM_MASK = (1 << TEAM_BITS) - 1;
        private const ushort INVASION_NPC_MASK = (1 << INVASION_NPC_BITS) - 1;

        // Events
        private const int HEALTH_BITS = 12;             // 0–4095
        private const int HEALTH_SHIFT = 0;
        private const ushort HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        public override void InitializeData(ref FNonPlayerCharacterData npcData, NonPlayerCharacterDefinition definition, ETeamID teamID, bool isInvasionNPC)
        {
            base.InitializeData(ref npcData, definition, teamID, isInvasionNPC);

            // Initialize Config
            SetTeamID(teamID, ref npcData);
            SetInvasionNPC(isInvasionNPC, ref npcData);

            // Initialize Events
            npcData.Events = 0;
            SetHealth(definition.MaxHealth, ref npcData);
        }

        // TeamID
        public ETeamID GetTeamID(ref FNonPlayerCharacterData npcData)
        {
            return (ETeamID)((npcData.Configuration >> TEAM_SHIFT) & TEAM_MASK);
        }

        public void SetTeamID(ETeamID teamID, ref FNonPlayerCharacterData npcData)
        {
            ushort config = npcData.Configuration;
            int teamValue = Mathf.Clamp((int)teamID, 0, TEAM_MASK);
            config = (ushort)((config & ~(TEAM_MASK << TEAM_SHIFT)) | (teamValue << TEAM_SHIFT));
            npcData.Configuration = config;
        }

        // Invasion NPC
        public bool IsInvasionNPC(ref FNonPlayerCharacterData npcData)
        {
            return ((npcData.Configuration >> INVASION_NPC_SHIFT) & INVASION_NPC_MASK) == 1;
        }

        public void SetInvasionNPC(bool isInvasionNPC, ref FNonPlayerCharacterData npcData)
        {
            ushort config = npcData.Configuration;
            int invasionValue = isInvasionNPC ? 1 : 0;
            config = (ushort)((config & ~(INVASION_NPC_MASK << INVASION_NPC_SHIFT)) | (invasionValue << INVASION_NPC_SHIFT));
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
        public void ApplyDamage(ref FNonPlayerCharacterData npcData,
            NonPlayerCharacterDefinition definition,
            int damage, int hitReactIndex)
        {
            int currentHealth = GetHealth(ref npcData);
            SetHealth(currentHealth - damage, ref npcData);

            if (GetHealth(ref npcData) == 0)
            {
                SetNPCState(TryAssignState(ref npcData, ENonPlayerState.Dead), ref npcData);
            }
            else
            {
                SetNPCState(TryAssignState(ref npcData, ENonPlayerState.HitReact), ref npcData);
                npcData.AnimationIndex = hitReactIndex;
            }
        }
    }
}
