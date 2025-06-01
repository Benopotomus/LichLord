using Fusion;
using UnityEngine;
// Creates and manages a list of networked prop data

namespace LichLord.Props
{
    public class PropReplicationData : ContextBehaviour, INetActor
    {
        [Networked, Capacity(PropConstants.MAX_PROP_REPS_NETOBJECT)]
        private NetworkArray<FPropData> _propDatas { get; }

        [Networked]
        protected int _dataCount { get; set; }
        public int DataCount => _dataCount;

        protected ArrayReader<FPropData> _dataBufferReader;
        protected PropertyReader<int> _dataCountReader;

        public FNetObjectID NetObjectID
        {
            get => Object != null ? new FNetObjectID { guid = Object.Id } : default;
        }
        public ref FPropData GetPropData(int index)
        {
            return ref _propDatas.GetRef(index);
        }

        public void AddProp(PropRuntimeState propRuntimeState)
        {
            if (!HasFreeProp())
            {
                Debug.LogWarning("Trying to add a prop data to a chunk when there's no room");
                return;
            }

            ref FPropData data = ref _propDatas.GetRef(_dataCount);
            data.GUID = propRuntimeState.guid;
            data.DefinitionID = propRuntimeState.definitionId;
            data.Position = propRuntimeState.position;
            data.Rotation = propRuntimeState.rotation;
            data.IsActive = true;
            data.StateData = propRuntimeState.data;
            _dataCount++;
        }

        public bool HasFreeProp()
        {
            return _dataCount < PropConstants.MAX_PROP_REPS_NETOBJECT;
        }

        public override void Spawned()
        {
            _dataBufferReader = GetArrayReader<FPropData>(nameof(_propDatas));
            _dataCountReader = GetPropertyReader<int>(nameof(_dataCount));
        }

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            PropManager propManager = Context.PropManager;

            // Update PropManager.runtimePropStates for each prop
            for (int i = 0; i < PropConstants.MAX_PROP_REPS_NETOBJECT; i++)
            {
                ref FPropData data = ref _propDatas.GetRef(i);

                // Skip inactive or destroyed props
                if (!data.IsActive)
                    continue;

                propManager.OverrideRuntimeData(ref data);
            }
        }
    }
}
