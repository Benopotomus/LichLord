namespace LichLord.NonPlayerCharacters
{
    using System;
    using UnityEngine;

    public static class NonPlayerCharacterDataUtility
    {
        // Bit size constants
        private const int DEFINITION_BITS = 7;          // 0–127
        private const int NPC_STATE_BITS = 4;           // 0–15

        // Bit shifts and masks for Configuration (ushort)
        private const int DEFINITION_SHIFT = 0;

        private const byte DEFINITION_MASK = (1 << DEFINITION_BITS) - 1;

        // Bit shifts and masks for Condition (byte)
        private const int NPC_STATE_SHIFT = 0;
        private const byte NPC_STATE_MASK = (1 << NPC_STATE_BITS) - 1;

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

    }

    // Enums for completeness
    public enum ENPCState : byte
    {
        Inactive,
        Idle,
        Dead,
        HitReact,
        Maneuver_1,
        Maneuver_2,
        Maneuver_3,
        Maneuver_4,
        Harvest_1,
        Harvest_2,
        Harvest_3,
        Harvest_4,
        Interact_1,
        Interact_2,
        Stunned,
        Spawning,
    }

    public enum EAttitude : byte
    {
        None,
        Hostile,
        Friendly,
        Neutral,
    }
}
