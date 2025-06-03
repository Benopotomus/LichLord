using Fusion;
using LichLord.Props;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableReplicator : ContextBehaviour
    {
        [Networked, Capacity(BuildableConstants.MAX_BUILD_REPS)]
        private NetworkDictionary<int, FBuildableData> _buildDatas { get; }

        [Networked]
        protected int _dataCount { get; set; }
        public int DataCount => _dataCount;

        public override void Spawned()
        {
            base.Spawned();
            Context.BuildableManager.AddReplicator(this);
        }

        public bool TryGetBuildableData(int guid, out FBuildableData data)
        {
            return _buildDatas.TryGet(guid, out data);
        }

        public void UpdateBuildData(int guid, FBuildableData updatedData)
        {
            _buildDatas.Set(guid, updatedData);
        }

        public void AddBuildable(BuildableRuntimeState propRuntimeState, bool initializing = false)
        {
            if (_dataCount >= BuildableConstants.MAX_BUILD_REPS)
            {
                Debug.LogWarning("Trying to add a prop data to a replicator when there's no room");
                return;
            }

            FBuildableData data = new FBuildableData
            {
                GUID = propRuntimeState.guid,
                DefinitionID = propRuntimeState.definitionId,
                Position = propRuntimeState.position,
                Rotation = propRuntimeState.rotation,
                IsActive = true,
                StateData = propRuntimeState.stateData
            };

            _buildDatas.Add(propRuntimeState.guid, data);

            _dataCount++;
        }

        public bool HasFreeProp()
        {
            return _dataCount < BuildableConstants.MAX_BUILD_REPS;
        }
    }
}