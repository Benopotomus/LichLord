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

        [Networked]
        private int _totalEnemies { get; set; }

        public void AddReplicator(NonPlayerCharacterReplicator replicator)
        {
            _replicator = replicator;
        }

        public override void Spawned()
        {
            if (Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single)
            {
                var newReplicator = Runner.Spawn(_replicatorPrefab, Vector3.zero, Quaternion.identity);

                for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
                {
                    Vector3 randomPosition = new Vector3(
                        Random.Range(-0f, 10f),
                        1f, // Keep Y fixed
                        Random.Range(-0f, 10f)
                    );

                    SpawnNPC(randomPosition, Global.Tables.NonPlayerCharacterTable.TryGetDefinition(1), ETeamID.EnemiesTeamA);
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

        public void ApplyDamage(int guid, Vector3 impulse, int damage)
        {
            //_replicator.ApplyDamageToNPC(guid, impulse, damage);
            /*
            _replicator.RaiseEvent(new NonPlayerCharacterDamageEvent
            {
                guid = guid,
                impulse = impulse,
                damage = 9001
            });
            */
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