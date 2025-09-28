using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "AlchemyTableDataDefinition", menuName = "LichLord/Buildables/AlchemyTableDataDefinition")]
    public class AlchemyTableDataDefinition : DestructibleBuildableDataDefinition // 14 bits
    {
        protected const int ALCHEMYTABLE_STATE_BITS = 3;         // 0-7 
        protected const int CONTAINER_INDEX_BITS = 10;         // 0-1023
        //27 bits

        protected const int ALCHEMYTABLE_STATE_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        protected const int CONTAINER_INDEX_SHIFT = CONTAINER_INDEX_BITS + ALCHEMYTABLE_STATE_BITS;

        protected const int ALCHEMYTABLE_STATE_MASK = (1 << ALCHEMYTABLE_STATE_BITS) - 1;
        protected const int CONTAINER_INDEX_MASK = (1 << CONTAINER_INDEX_BITS) - 1;

        public override void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {
            // Initialize fields
            buildableData.DefinitionID = (ushort)definition.TableID; // Assuming definition has an ID
            buildableData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref buildableData);
            SetHealth(definition.MaxHealth, ref buildableData); // Default health, adjust as needed
            SetAlchemyTableState(EAlchemyTableState.None, ref buildableData);
        }

        // Container State Index
        public EAlchemyTableState GetAlchemyTableState(ref FBuildableData data)
        {
            return (EAlchemyTableState)((data.StateData >> ALCHEMYTABLE_STATE_SHIFT) & ALCHEMYTABLE_STATE_MASK);
        }

        public void SetAlchemyTableState(EAlchemyTableState newContainerState, ref FBuildableData buildableData)
        {
            int stateData = buildableData.StateData;
            stateData = (stateData & ~(ALCHEMYTABLE_STATE_MASK << ALCHEMYTABLE_STATE_SHIFT)) | ((int)(newContainerState) << ALCHEMYTABLE_STATE_SHIFT);
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
            switch (GetAlchemyTableState(ref data))
            {
                case EAlchemyTableState.None:
                    return false;
                case EAlchemyTableState.Interacting:
                case EAlchemyTableState.Open:
                case EAlchemyTableState.Working:
                    return true;
            }

            return false;
        }

    }

    public enum EAlchemyTableState
    {
        None,
        Interacting,
        Open,
        Working,
    }
}