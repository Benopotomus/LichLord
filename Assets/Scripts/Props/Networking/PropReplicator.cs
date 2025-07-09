using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LichLord.Props
{
    public class PropReplicator : ContextBehaviour, IStateAuthorityChanged
    {
        [Networked, Capacity(PropConstants.MAX_PROP_REPS)]
        private NetworkArray<FPropData> _propDatas { get; }

        private Dictionary<int, int> _linkingDictionary = new Dictionary<int, int>();

        private HashSet<int> _freeIndices = new HashSet<int>();
        public IReadOnlyCollection<int> FreeIndices => _freeIndices;

        public override void Spawned()
        {
            base.Spawned();

            for (int i = 0; i < PropConstants.MAX_PROP_REPS; i++)
            {
                _freeIndices.Add(i); // Initially, all indices are free
            }
        }

        public void StateAuthorityChanged()
        {
            Debug.Log($"StateAuthority Changed, HasStateAuthority: {HasStateAuthority}");
            if (!HasStateAuthority)
                return;

            RebuildFreeIndices();
        }

        private void RebuildFreeIndices()
        {
            _freeIndices.Clear();
            _linkingDictionary.Clear(); // Ensure dictionary is consistent

            for (int i = 0; i < _propDatas.Length; i++)
            {
                ref FPropData data = ref _propDatas.GetRef(i);
                if (!data.IsValid())
                {
                    _freeIndices.Add(i);
                }
                else
                {
                    _linkingDictionary[data.GUID] = i; // Map GUID to index
                }
            }
        }

        public bool TryGetPropData(int guid, out FPropData data)
        {
            data = default;
            if (_linkingDictionary.TryGetValue(guid, out int index))
            {
                data = _propDatas.Get(index);
                return true;
            }
            return false;
        }

        public void UpdatePropData(int guid, FPropData updatedData)
        {
            if (!_linkingDictionary.TryGetValue(guid, out int index))
            {
                Debug.LogWarning($"Cannot update prop with GUID {guid}: not found.");
                return;
            }

            if (updatedData.GUID != guid)
            {
                Debug.LogError($"Updated data GUID {updatedData.GUID} does not match requested GUID {guid}.");
                return;
            }

            _propDatas.Set(index, updatedData);
            if (!updatedData.IsValid())
            {
                _freeIndices.Add(index);
                _linkingDictionary.Remove(guid);
            }
            else
            {
                _freeIndices.Remove(index);
                _linkingDictionary[guid] = index;
            }
        }

        public void AddProp(PropRuntimeState propRuntimeState, bool initializing = false)
        {
            if (_freeIndices.Count == 0)
            {
                Debug.LogError("No free indices available to add prop.");
                return;
            }

            // Validate GUID range
            if (propRuntimeState.guid < 0)
            {
                Debug.LogError($"Invalid GUID {propRuntimeState.guid}. Must be non-negative.");
                return;
            }

            int index = _freeIndices.First();
            _freeIndices.Remove(index);

            FPropData data = new FPropData
            {
                GUID = propRuntimeState.guid,
                DefinitionID = propRuntimeState.definitionId,
                StateData = propRuntimeState.Data.StateData
            };

            _propDatas.Set(index, data);
            _linkingDictionary[propRuntimeState.guid] = index;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Runner.IsForward || !Runner.IsFirstTick || !Context.IsGameplayActive())
                return;

            int tick = Runner.Tick;

            for (int i = 0; i < _propDatas.Length; i++)
            {
                ref FPropData data = ref _propDatas.GetRef(i);
                if (!data.IsValid())
                    continue;


            }
        }
    }
}