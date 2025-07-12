using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterManagerDebug : ContextBehaviour
    {
        [Header("Debug Spawning")]
        [SerializeField] private NonPlayerCharacterDefinition _debugSpawnDefinition;
        [SerializeField] private int _initialSpawnCount = 0;
        [SerializeField] private bool _debugStreamRevive;
        [SerializeField] private int _streamSpawnCount = 0;

        [SerializeField] private Vector3 _debugSpawnPosition = new Vector3(1000, 0, 1000);
        [SerializeField] private Transform _debugSpawnTransform;

        public void OnSpawned()
        {
            if (_debugSpawnTransform != null) 
                _debugSpawnPosition = _debugSpawnTransform.position;

            if (Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single)
            {
                for (int i = 0; i < _initialSpawnCount; i++)
                {
                    Vector3 randomPosition = new Vector3(
                        Random.Range(-10f, 10f),
                        1f, // Keep Y fixed
                        Random.Range(-10f, 10f)
                    );

                    randomPosition += _debugSpawnPosition + new Vector3(35, 0, 0);
                    Context.NonPlayerCharacterManager.SpawnNPC(randomPosition, _debugSpawnDefinition, ETeamID.EnemiesTeamA);
                }

                for (int i = 0; i < _initialSpawnCount; i++)
                {
                    Vector3 randomPosition = new Vector3(
                        Random.Range(-10f, 10f),
                        1f, // Keep Y fixed
                        Random.Range(-10f, 10f)
                    );

                    randomPosition += _debugSpawnPosition + new Vector3(-35, 0, 0);

                    Context.NonPlayerCharacterManager.SpawnNPC(randomPosition, _debugSpawnDefinition, ETeamID.EnemiesTeamB);
                }
            }
        }

        bool flip = false;
        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (!_debugStreamRevive)
                return;

            if (Runner.Tick % 64 != 0)
                return;

            if (flip)
            {
                for (int i = 0; i < _streamSpawnCount; i++)
                {

                    Vector3 randomPosition = new Vector3(
                        Random.Range(-10f, 10f),
                        1f, // Keep Y fixed
                        Random.Range(-10f, 10f)
                    );

                    randomPosition += _debugSpawnPosition + new Vector3(35, 0, 0);
                    Context.NonPlayerCharacterManager.SpawnNPC(randomPosition, _debugSpawnDefinition, ETeamID.EnemiesTeamA);
                }
                flip = false;
            }
            else
            {
                for (int i = 0; i < _streamSpawnCount; i++)
                {
                    Vector3 randomPosition = new Vector3(
                        Random.Range(-10f, 10f),
                        1f, // Keep Y fixed
                        Random.Range(-10f, 10f)
                    );

                    randomPosition += _debugSpawnPosition + new Vector3(-35, 0, 0);

                    Context.NonPlayerCharacterManager.SpawnNPC(randomPosition, _debugSpawnDefinition, ETeamID.EnemiesTeamB);
                }
                flip = true;
            }
        }
    }
}