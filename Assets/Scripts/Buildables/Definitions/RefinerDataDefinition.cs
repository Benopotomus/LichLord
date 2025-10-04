using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "RefinerDataDefinition", menuName = "LichLord/Buildables/RefinerDataDefinition")]
    public class RefinerDataDefinition : ContainerDataDefinition // 23 bits
    {

        protected const int REFINER_STATE_BITS = 2; // 0-3
        protected const int REFINDER_PROGRESS_BITS = 4; // 0-15
        //29 bits.. three bits left

        protected const int REFINER_STATE_SHIFT = CONTAINER_INDEX_SHIFT + CONTAINER_INDEX_BITS;
        protected const int REFINDER_PROGRESS_SHIFT = REFINER_STATE_SHIFT + REFINER_STATE_BITS;

        protected const int REFINER_STATE_MASK = (1 << REFINER_STATE_BITS) - 1;
        protected const int REFINDER_PROGRESS_MASK = (1 << REFINDER_PROGRESS_BITS) - 1;

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

        public ERefinerState GetRefinerState(ref FBuildableData buildableData)
        {
            return (ERefinerState)((buildableData.StateData >> REFINER_STATE_SHIFT) & REFINER_STATE_MASK);
        }

        public void SetRefinerState(ERefinerState newRefinerState, ref FBuildableData propData)
        {
            int stateData = propData.StateData;
            int stateValue = Mathf.Clamp((int)newRefinerState, 0, REFINER_STATE_MASK);
            stateData = (stateData & ~(REFINER_STATE_MASK << REFINER_STATE_SHIFT)) | (stateValue << REFINER_STATE_SHIFT);
            propData.StateData = stateData;
        }

        // Refiner Progress Index

        public int GetRefinerProgress(ref FBuildableData data)
        {
            return (data.StateData >> REFINDER_PROGRESS_SHIFT) & REFINDER_PROGRESS_MASK;
        }

        public void SetRefinerProgress(int index, ref FBuildableData buildableData)
        {
            int stateData = buildableData.StateData;
            stateData = (stateData & ~(REFINDER_PROGRESS_MASK << REFINDER_PROGRESS_SHIFT)) | (index << REFINDER_PROGRESS_SHIFT);
            buildableData.StateData = stateData;
        }
    }

    public enum ERefinerState
    { 
        None,
        Active,
        Complete,
    }
}