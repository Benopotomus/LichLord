using LichLord.NonPlayerCharacters;
using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "CryptDataDefinition", menuName = "LichLord/Buildables/CryptDataDefinition")]
    public class CryptDataDefinition : DestructibleBuildableDataDefinition // 14 bits
    {
        [SerializeField] private NonPlayerCharacterDefinition _workerDefinition;
        public NonPlayerCharacterDefinition WorkerDefinition => _workerDefinition;

        [SerializeField] private int _workerRespawnTicks = 320;
        public int WorkerRespawnTicks => _workerRespawnTicks;

        protected const int WORKER_STATE_BITS = 3;         // 0-7 =w 17
        protected const int WORKER_INDEX_BITS = 7;         // 0-127 = 24
        protected const int IS_INTERACTING_BITS = 1; // = 25
        //25 bits

        protected const int WORKER_STATE_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        protected const int WORKER_INDEX_SHIFT = WORKER_STATE_SHIFT + WORKER_STATE_BITS;
        protected const int IS_INTERACTING_SHIFT = WORKER_INDEX_SHIFT + WORKER_INDEX_BITS;

        protected const int WORKER_STATE_MASK = (1 << WORKER_STATE_BITS) - 1;
        protected const int WORKER_INDEX_MASK = (1 << WORKER_INDEX_BITS) - 1;
        protected const int IS_INTERACTING_MASK = (1 << IS_INTERACTING_BITS) - 1;

        public override void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {
            // Initialize fields
            buildableData.DefinitionID = (ushort)definition.TableID; // Assuming definition has an ID
            buildableData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref buildableData);
            SetHealth(MaxHealth, ref buildableData); // Default health, adjust as needed
            SetIsInteracting(false, ref buildableData);
            SetWorkerState(EWorkerState.None, ref buildableData);
        }

        // NPC Index
        public int GetWorkerIndex(ref FBuildableData data)
        {
            return (data.StateData >> WORKER_INDEX_SHIFT) & WORKER_INDEX_MASK;
        }

        public void SetWorkerIndex(int index, ref FBuildableData buildableData)
        {
           int stateData = buildableData.StateData;
           stateData = (stateData & ~(WORKER_INDEX_MASK << WORKER_INDEX_SHIFT)) | (index << WORKER_INDEX_SHIFT);
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

        // Worker State
        public EWorkerState GetWorkerState(ref FBuildableData data)
        {
            return (EWorkerState)((data.StateData >> WORKER_STATE_SHIFT) & WORKER_STATE_MASK);
        }

        public void SetWorkerState(EWorkerState newWorkerState, ref FBuildableData buildableData)
        {
            int stateData = buildableData.StateData;
            stateData = (stateData & ~(WORKER_STATE_MASK << WORKER_STATE_SHIFT)) | ((int)(newWorkerState) << WORKER_STATE_SHIFT);
            buildableData.StateData = stateData;
        }
    }

    public enum EWorkerState
    { 
        None,
        Cooldown,
        Spawning,
        WorkerActive,
    }
}