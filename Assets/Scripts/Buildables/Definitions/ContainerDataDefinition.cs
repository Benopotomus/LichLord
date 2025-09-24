using LichLord.NonPlayerCharacters;
using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "ContainerDataDefinition", menuName = "LichLord/Buildables/ContainerDataDefinition")]
    public class ContainerDataDefinition : DestructibleBuildableDataDefinition // 14 bits
    {

        protected const int CONTAINER_STATE_BITS = 3;         // 0-7 
        protected const int CONTAINER_INDEX_BITS = 10;         // 0-1023
        //27 bits

        protected const int CONTAINER_STATE_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        protected const int CONTAINER_INDEX_SHIFT = CONTAINER_INDEX_BITS + CONTAINER_STATE_BITS;

        protected const int CONTAINER_STATE_MASK = (1 << CONTAINER_STATE_BITS) - 1;
        protected const int CONTAINER_INDEX_MASK = (1 << CONTAINER_INDEX_BITS) - 1;

        public override void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {
            // Initialize fields
            buildableData.DefinitionID = (ushort)definition.TableID; // Assuming definition has an ID
            buildableData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref buildableData);
            SetHealth(MaxHealth, ref buildableData); // Default health, adjust as needed
            SetContainerState(EContainerState.None, ref buildableData);
        }

        // Container State Index
        public EContainerState GetContainerState(ref FBuildableData data)
        {
            return (EContainerState)((data.StateData >> CONTAINER_STATE_SHIFT) & CONTAINER_STATE_MASK);
        }

        public void SetContainerState(EContainerState newContainerState, ref FBuildableData buildableData)
        {
            int stateData = buildableData.StateData;
            stateData = (stateData & ~(CONTAINER_STATE_MASK << CONTAINER_STATE_SHIFT)) | ((int)(newContainerState) << CONTAINER_STATE_SHIFT);
            buildableData.StateData = stateData;
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
            switch (GetContainerState(ref data))
            {
                case EContainerState.None:
                    return false;
                case EContainerState.Interacting:
                case EContainerState.Open:
                case EContainerState.Working:
                    return true;
            }

            return false;
        }

    }

    public enum EContainerState
    { 
        None,
        Interacting,
        Open,
        Working,
    }
}