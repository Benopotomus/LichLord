using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterManager : ContextBehaviour
    {
        [SerializeField] private NonPlayerCharacterSpawner _spawner;
        [SerializeField] private NonPlayerCharacterReplicator _replicatorPrefab;
        [SerializeField] private List<NonPlayerCharacterReplicator> _replicators = new List<NonPlayerCharacterReplicator>();

        [SerializeField] private Dictionary<int, NonPlayerCharacterRuntimeState> _deltaStates = new Dictionary<int, NonPlayerCharacterRuntimeState>();

        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private float despawnRadius = 60f;

        public void AddReplicator(NonPlayerCharacterReplicator replicator)
        {
            _replicators.Add(replicator);
        }

        public override void Spawned()
        {
            if (Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single)
            {
                for (int i = 0; i < 1; i++)
                {
                    Vector3 randomPosition = new Vector3(
                        Random.Range(-15f, 15f),
                        1f, // Keep Y fixed
                        Random.Range(-15f, 15f)
                    );

                    SpawnNPC(randomPosition, Global.Tables.NonPlayerCharacterTable.TryGetDefinition(1));
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

        public void SpawnNPC(Vector3 spawnPos, NonPlayerCharacterDefinition definition)
        {
            if (Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single)
            {
                EnsureEmptyReplicator();

                // Get a free replicator and add its data
                for (int i = 0; i < _replicators.Count; i++)
                {
                    NonPlayerCharacterReplicator replicator = _replicators[i];
                    if (replicator.HasFreeSlot())
                    {
                        FNonPlayerCharacterData data = new FNonPlayerCharacterData
                        {
                            DefinitionID = definition.TableID,
                            Transform = new FWorldTransform
                            {
                                Position = spawnPos,
                                Rotation = Quaternion.identity,
                            },
                            //Velocity = Vector3.zero,
                            StateData = 0,
                            Health = 0,
                        };

                        replicator.AddNPC(data);
                        break;
                    }
                }
            }
        }

        private void EnsureEmptyReplicator()
        {
            // Check if there is at least one completely empty replicator (zero entries)
            bool hasEmptyReplicator = false;
            foreach (var replicator in _replicators)
            {
                if (replicator.DataCount == 0)
                {
                    hasEmptyReplicator = true;
                    break;
                }
            }

            // If no empty replicator exists, spawn a new one
            if (!hasEmptyReplicator)
            {
                var newReplicator = Runner.Spawn(_replicatorPrefab, Vector3.zero, Quaternion.identity);
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