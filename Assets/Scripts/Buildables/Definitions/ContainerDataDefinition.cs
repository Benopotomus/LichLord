using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "ContainerDataDefinition", menuName = "LichLord/Buildables/ContainerDataDefinition")]
    public class ContainerDataDefinition : DestructibleBuildableDataDefinition // 12 bits
    {

        protected const int IS_INTERACTING_BITS = 1;
        protected const int CONTAINER_INDEX_BITS = 10;         // 0-1023
        //23 bits

        protected const int IS_INTERACTING_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        protected const int CONTAINER_INDEX_SHIFT = IS_INTERACTING_SHIFT + IS_INTERACTING_BITS;

        protected const int IS_INTERACTING_MASK = (1 << IS_INTERACTING_BITS) - 1;
        protected const int CONTAINER_INDEX_MASK = (1 << CONTAINER_INDEX_BITS) - 1;

        public override void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {
            // Initialize fields
            buildableData.DefinitionID = (ushort)definition.TableID; // Assuming definition has an ID
            buildableData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref buildableData);
            SetHealth(definition.MaxHealth, ref buildableData); // Default health, adjust as needed
        }

        // Container Index

        public int GetContainerIndex(ref FBuildableData data)
        {
            return (data.StateData >> CONTAINER_INDEX_SHIFT) & CONTAINER_INDEX_MASK;
        }

        public void SetContainerIndex(int index, ref FBuildableData buildableData)
        {
            int stateData = buildableData.StateData;
            stateData = (stateData & ~(CONTAINER_INDEX_MASK << CONTAINER_INDEX_SHIFT)) | (index << CONTAINER_INDEX_SHIFT);
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