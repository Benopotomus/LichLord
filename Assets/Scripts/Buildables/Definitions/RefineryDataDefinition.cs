using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "RefineryDataDefinition", menuName = "LichLord/Buildables/RefineryDataDefinition")]
    public class RefineryDataDefinition : ContainerDataDefinition // 23 bits
    {

        protected const int REFINERY_STATE_BITS = 2; // 0-3
        protected const int REFINERY_PROGRESS_BITS = 4; // 0-16
        //29 bits
        //3 Valid Recipe

        protected const int REFINERY_STATE_SHIFT = CONTAINER_INDEX_SHIFT + CONTAINER_INDEX_BITS;
        protected const int REFINERY_PROGRESS_SHIFT = REFINERY_STATE_SHIFT + REFINERY_STATE_BITS;

        protected const int REFINERY_STATE_MASK = (1 << REFINERY_STATE_BITS) - 1;
        protected const int REFINDERY_PROGRESS_MASK = (1 << REFINERY_PROGRESS_BITS) - 1;

        public override void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {
            // Initialize fields
            buildableData.DefinitionID = (ushort)definition.TableID; // Assuming definition has an ID
            buildableData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref buildableData);
            SetHealth(definition.MaxHealth, ref buildableData); // Default health, adjust as needed
        }

        // Refiner State

        public ERefineryState GetRefineryState(ref FBuildableData buildableData)
        {
            return (ERefineryState)((buildableData.StateData >> REFINERY_STATE_SHIFT) & REFINERY_STATE_MASK);
        }

        public void SetRefineryState(ERefineryState newRefinerState, ref FBuildableData propData)
        {
            int stateData = propData.StateData;
            int stateValue = Mathf.Clamp((int)newRefinerState, 0, REFINERY_STATE_MASK);
            stateData = (stateData & ~(REFINERY_STATE_MASK << REFINERY_STATE_SHIFT)) | (stateValue << REFINERY_STATE_SHIFT);
            propData.StateData = stateData;
        }

        // Refiner Progress Index

        public int GetRefineryProgress(ref FBuildableData data)
        {
            return (data.StateData >> REFINERY_PROGRESS_SHIFT) & REFINDERY_PROGRESS_MASK;
        }

        public void SetRefineryProgress(int index, ref FBuildableData buildableData)
        {
            int stateData = buildableData.StateData;
            stateData = (stateData & ~(REFINDERY_PROGRESS_MASK << REFINERY_PROGRESS_SHIFT)) | (index << REFINERY_PROGRESS_SHIFT);
            buildableData.StateData = stateData;
        }
    }

    public enum ERefineryState
    { 
        None,
        Active,
        Complete,
        Disabled,
    }
}