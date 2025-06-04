using Fusion;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterReplicator : ContextBehaviour
    {
        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        private NetworkDictionary<int, FNonPlayerCharacterData> _npcDatas { get; }

        [Networked]
        protected int _dataCount { get; set; }
        public int DataCount => _dataCount;

        public override void Spawned()
        {
            base.Spawned();
            Context.NonPlayerCharacterManager.AddReplicator(this);
        }

        public bool TryGetNPCData(int guid, out FNonPlayerCharacterData data)
        {
            return _npcDatas.TryGet(guid, out data);
        }

        public void UpdatePropData(int guid, FNonPlayerCharacterData updatedData)
        {
            _npcDatas.Set(guid, updatedData);
        }

        public void AddProp(NonPlayerCharacterRuntimeState runtimeState, bool initializing = false)
        {
            if (_dataCount >= NonPlayerCharacterConstants.MAX_NPC_REPS)
            {
                Debug.LogWarning("Trying to add a prop data to a replicator when there's no room");
                return;
            }

            FNonPlayerCharacterData data = new FNonPlayerCharacterData
            {
                GUID = runtimeState.guid,
                DefinitionID = runtimeState.definitionId,
                Transform = new FWorldTransform 
                {   
                    Position = runtimeState.position, 
                    Rotation = runtimeState.rotation
                },
                StateData = runtimeState.stateData,
                Health = runtimeState.health,
            };

            _npcDatas.Add(runtimeState.guid, data);

            _dataCount++;
        }

        public bool HasFreeSlot()
        {
            return _dataCount < NonPlayerCharacterConstants.MAX_NPC_REPS;
        }
    }
}