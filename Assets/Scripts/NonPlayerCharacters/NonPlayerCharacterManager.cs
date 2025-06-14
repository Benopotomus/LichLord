using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterManager : ContextBehaviour
    {
        [SerializeField] private NonPlayerCharacterSpawner _spawner;
        [SerializeField] private NonPlayerCharacterReplicator _replicatorPrefab;
        [SerializeField] private NonPlayerCharacterReplicator _replicator;

        [SerializeField] private Dictionary<int, FNonPlayerCharacterData> _deltaStates = new Dictionary<int, FNonPlayerCharacterData>();

        [SerializeField] private int _debugSpawnCount = 0;
        public void AddReplicator(NonPlayerCharacterReplicator replicator)
        {
            _replicator = replicator;
        }

        public override void Spawned()
        {
            if (Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single)
            {
                var newReplicator = Runner.Spawn(_replicatorPrefab, Vector3.zero, Quaternion.identity);

                for (int i = 0; i < _debugSpawnCount; i++)
                {
                    Vector3 randomPosition = new Vector3(
                        Random.Range(-10f, 10f),
                        1f, // Keep Y fixed
                        Random.Range(-10f, 10f)
                    );

                    randomPosition += new Vector3(35, 0, 0);
                    SpawnNPC(randomPosition, Global.Tables.NonPlayerCharacterTable.TryGetDefinition(1), ETeamID.EnemiesTeamA);
                }

                for (int i = 0; i < _debugSpawnCount; i++)
                {
                    Vector3 randomPosition = new Vector3(
                        Random.Range(-10f, 10f),
                        1f, // Keep Y fixed
                        Random.Range(-10f, 10f)
                    );

                    randomPosition += new Vector3(-35, 0, 0);

                    SpawnNPC(randomPosition, Global.Tables.NonPlayerCharacterTable.TryGetDefinition(1), ETeamID.EnemiesTeamB);
                }
            }
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
            if (Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single)
            {
                if (_replicator == null)
                    return;

                int freeIndex = _replicator.GetFreeIndex();
                if (freeIndex == -1)
                {
                    Debug.Log("Can't Spawn NPC No Free Index");
                    return;
                }

                FNonPlayerCharacterData data = new FNonPlayerCharacterData();
                NonPlayerCharacterDataUtility.InitializeData(ref data, definition, freeIndex, teamID);

                data.Position = spawnPos;
                data.Rotation = Quaternion.identity;

                _deltaStates[freeIndex] = data; // Store full state for persistence
                _replicator.UpdateNPCData(data);
            }
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