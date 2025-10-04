using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "StockpileDataDefinition", menuName = "LichLord/Buildables/StockpileDataDefinition")]
    public class StockpileDataDefinition : DestructibleBuildableDataDefinition
    {

        protected const int STOCKPILE_INDEX_BITS = 7;         // 0-127
        protected const int IS_INTERACTING_BITS = 1;

        protected const int STOCKPILE_INDEX_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        protected const int IS_INTERACTING_SHIFT = STOCKPILE_INDEX_SHIFT + STOCKPILE_INDEX_BITS;

        protected const int STOCKPILE_INDEX_MASK = (1 << STOCKPILE_INDEX_BITS) - 1;
        protected const int IS_INTERACTING_MASK = (1 << IS_INTERACTING_BITS) - 1;

        public override void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {
            // Initialize fields
            buildableData.DefinitionID = (ushort)definition.TableID; // Assuming definition has an ID
            buildableData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref buildableData);
            SetHealth(definition.MaxHealth, ref buildableData); // Default health, adjust as needed
        }

        // Stockpile Index
        public int GetStockpileIndex(ref FBuildableData data)
        {
            return (data.StateData >> STOCKPILE_INDEX_SHIFT) & STOCKPILE_INDEX_MASK;
        }

        public void SetStockpileIndex(int index, ref FBuildableData buildableData)
        {
            int stateData = buildableData.StateData;
            stateData = (stateData & ~(STOCKPILE_INDEX_MASK << STOCKPILE_INDEX_SHIFT)) | (index << STOCKPILE_INDEX_SHIFT);
            buildableData.StateData = stateData;
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
}