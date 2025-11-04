using UnityEngine;

//14 bits

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "DestructibleBuildableDataDefinition", menuName = "LichLord/Buildables/DestructibleBuildableDataDefinition")]
    public class DestructibleBuildableDataDefinition : BuildableDataDefinition
    {
        protected const int HEALTH_BITS = 10;         // 0-1023

        protected const int HEALTH_SHIFT = STATE_SHIFT + STATE_BITS;

        protected const int HEALTH_MASK = (1 << HEALTH_BITS) - 1;
        // 12 bits

        public override void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {
            // Initialize fields
            buildableData.DefinitionID = (ushort)definition.TableID; // Assuming definition has an ID
            buildableData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref buildableData);
            SetHealth(definition.MaxHealth, ref buildableData); // Default health, adjust as needed
        }

        // Health
        public int GetHealth(ref FBuildableData buildableData)
        {
            return (buildableData.StateData >> HEALTH_SHIFT) & HEALTH_MASK;
        }

        public void SetHealth(int health, ref FBuildableData buildableData)
        {
            int stateData = buildableData.StateData;
            health = Mathf.Clamp(health, 0, HEALTH_MASK);
            stateData = (stateData & ~(HEALTH_MASK << HEALTH_SHIFT)) | (health << HEALTH_SHIFT);
            buildableData.StateData = stateData;
        }


    }


}