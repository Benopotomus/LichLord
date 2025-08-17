using LichLord.Buildables;
using UnityEngine;

//25 bits

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "CryptDataDefinition", menuName = "LichLord/Buildables/CryptDataDefinition")]
    public class CryptDataDefinition : DestructibleBuildableDataDefinition
    {
        protected const int SPAWN_STATE_BITS = 3;         // 0-7 = 17
        protected const int NPC_INDEX_BITS = 7;         // 0-128 = 24
        protected const int IS_INTERACTING_BITS = 1; // = 25

        protected const int SPAWN_STATE_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        protected const int NPC_INDEX_SHIFT = SPAWN_STATE_SHIFT + SPAWN_STATE_BITS;
        protected const int IS_INTERACTING_SHIFT = SPAWN_STATE_SHIFT + SPAWN_STATE_BITS;

        protected const int SPAWN_STATE_MASK = (1 << SPAWN_STATE_BITS) - 1;
        protected const int NPC_INDEX_MASK = (1 << NPC_INDEX_BITS) - 1;
        protected const int IS_INTERACTING_MASK = (1 << IS_INTERACTING_BITS) - 1;

        public override void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {
            // Initialize fields
            buildableData.DefinitionID = (ushort)definition.TableID; // Assuming definition has an ID
            buildableData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref buildableData);
            SetHealth(MaxHealth, ref buildableData); // Default health, adjust as needed
        }

        // NPC Index
        public int GetNPCIndex(ref FBuildableData data)
        {
            return (data.StateData >> NPC_INDEX_SHIFT) & NPC_INDEX_MASK;
        }

        public void SetNPCIndex(int index, ref FBuildableData buildableData)
        {
           // int stateData = buildableData.StateData;
           // stateData = (stateData & ~(STOCKPILE_INDEX_MASK << STOCKPILE_INDEX_SHIFT)) | (index << STOCKPILE_INDEX_SHIFT);
           // buildableData.StateData = stateData;
        }

        // Interacting
        public bool GetIsInteracting(ref FBuildableData data)
        {
            return (data.StateData & (IS_INTERACTING_MASK << IS_INTERACTING_SHIFT)) != 0;
        }

        public void SetIsInteracting(bool isInteracting, ref FBuildableData data)
        {
            int stateData = data.StateData;
            stateData &= ~(IS_INTERACTING_MASK << IS_INTERACTING_SHIFT); // clear bit
            if (isInteracting)
                stateData |= (1 << IS_INTERACTING_SHIFT);
            data.StateData = stateData;
        }
    }

    public enum ECryptSpawnState
    { 
        Cooldown,
        SpawningNPC,
        NPC_Active,
        NPC_Inside,
    }
}