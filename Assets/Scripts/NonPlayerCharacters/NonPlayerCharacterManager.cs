using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterManager : ContextBehaviour
    {
        [SerializeField] private NonPlayerCharacterSpawner _spawner;
        [SerializeField] private NonPlayerCharacterReplicator _replicatorPrefab;
        [SerializeField] private NonPlayerCharacterManagerDebug _debug;

        [SerializeField] private Dictionary<int, FNonPlayerCharacterData> _deltaStates = new Dictionary<int, FNonPlayerCharacterData>();

        private List<NonPlayerCharacterReplicator> _replicators = new List<NonPlayerCharacterReplicator>();

        public void AddReplicator(NonPlayerCharacterReplicator replicator)
        {
            if (!_replicators.Contains(replicator))
            {
                _replicators.Add(replicator);
            }
        }

        public override void Spawned()
        {
           _debug.OnSpawned();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            if (runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single)
            {
                //saveLoadManager.SaveRuntimeState(_deltaStates);
            }
        }

        public void SpawnNPC(Vector3 spawnPos, NonPlayerCharacterDefinition definition, ETeamID teamID)
        {
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
            {
                Debug.Log("Cannot spawn, I'm not the master client");
                return;
            }

            NonPlayerCharacterReplicator replicator = GetReplicatorWithFreeSlots();
            if(replicator == null) 
                return;

            int freeIndex = replicator.GetFreeIndex();
            if (freeIndex == -1)
            {
                Debug.Log("Can't Spawn NPC No Free Index");
                return;
            }

            FNonPlayerCharacterData data = new FNonPlayerCharacterData();
            NonPlayerCharacterDataUtility.InitializeData(ref data, definition, teamID);

            data.Position = spawnPos;
            data.Rotation = Quaternion.identity;

            _deltaStates[freeIndex] = data; // Store full state for persistence
            replicator.UpdateNPCData(ref data, freeIndex);
        }

        public NonPlayerCharacterReplicator GetReplicatorWithFreeSlots()
        {
            foreach (var replicator in _replicators)
            {
                if (replicator.FreeIndices.Count > 0)
                    return replicator;
            }

            if (_replicators.Count < NonPlayerCharacterConstants.MAX_REPLICATORS)
            {
                var newReplicator = Runner.Spawn(_replicatorPrefab, Vector3.zero, Quaternion.identity);
                if (newReplicator != null)
                {
                    AddReplicator(newReplicator);
                }
            }

            Debug.Log("No replicator with free slots found");
            return null;
        }

        public class NPCLoadState
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
        }

        public enum ELoadState
        {
            None,
            Loading,
            Loaded,
            Unloading,
        }
    }
}