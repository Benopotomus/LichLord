using Fusion;
using UnityEngine;

namespace LichLord.Props
{
    public class PropReplicationData : ContextBehaviour
    {
        [Networked, Capacity(PropConstants.MAX_PROP_REPS_NETOBJECT)]
        private NetworkDictionary<int, FPropData> _propDatas { get; }

        [Networked]
        protected int _dataCount { get; set; }
        public int DataCount => _dataCount;

        public bool TryGetPropData(int guid, out FPropData data)
        {
            return _propDatas.TryGet(guid, out data);
        }

        public void UpdatePropData(int guid, FPropData updatedData)
        {
            _propDatas.Set(guid, updatedData);
        }

        public void AddProp(PropRuntimeState propRuntimeState)
        {
            if (_dataCount >= PropConstants.MAX_PROP_REPS_NETOBJECT)
            {
                Debug.LogWarning("Trying to add a prop data to a replicator when there's no room");
                return;
            }

            FPropData data = new FPropData
            {
                GUID = propRuntimeState.guid,
                DefinitionID = propRuntimeState.definitionId,
                Position = propRuntimeState.position,
                Rotation = propRuntimeState.rotation,
                IsActive = true,
                StateData = propRuntimeState.stateData
            };

            _propDatas.Add(propRuntimeState.guid, data);
            Context.PropManager.OverrideRuntimeData(ref data);
            _dataCount++;
        }

        public bool HasFreeProp()
        {
            return _dataCount < PropConstants.MAX_PROP_REPS_NETOBJECT;
        }

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            foreach (var kvp in _propDatas)
            {
                FPropData data = kvp.Value;
                if (!data.IsActive)
                    continue;

                Context.PropManager.OverrideRuntimeData(ref data);
            }
        }
    }
}