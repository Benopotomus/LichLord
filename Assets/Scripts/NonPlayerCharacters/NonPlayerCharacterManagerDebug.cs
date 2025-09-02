using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterManagerDebug : ContextBehaviour
    {
        [Header("Debug Spawning")]
        [SerializeField] private List<NonPlayerCharacterDefinition> _debugSpawnDefinitions;
        [SerializeField] private int _initialSpawnCount = 0;
        [SerializeField] private bool _debugStreamRevive;
        [SerializeField] private int _streamSpawnCount = 0;

        [SerializeField] private Vector3 _debugSpawnPosition = new Vector3(1000, 0, 1000);
        [SerializeField] private Transform _debugSpawnTransform;

        public void OnSpawned()
        {
            if (_debugSpawnTransform != null) 
                _debugSpawnPosition = _debugSpawnTransform.position;

            var spawnDef = GetRandomSpawnDefinition();

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
                    Context.NonPlayerCharacterManager.SpawnNPC(randomPosition, 
                        spawnDef, 
                        ENPCSpawnType.
                        Invasion, 
                        ETeamID.EnemiesTeamA, 
                        EAttitude.Hostile);
                }
                
                for (int i = 0; i < _initialSpawnCount; i++)
                {
                    Vector3 randomPosition = new Vector3(
                        Random.Range(-10f, 10f),
                        1f, // Keep Y fixed
                        Random.Range(-10f, 10f)
                    );

                    randomPosition += _debugSpawnPosition + new Vector3(-35, 0, 0);

                    Context.NonPlayerCharacterManager.SpawnNPC(randomPosition,
                        spawnDef, 
                        ENPCSpawnType.Invasion, 
                        ETeamID.EnemiesTeamB,
                        EAttitude.Hostile);
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

            var spawnDef = GetRandomSpawnDefinition();
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
                    Context.NonPlayerCharacterManager.SpawnNPC(randomPosition, spawnDef, ENPCSpawnType.Invasion, ETeamID.EnemiesTeamA, EAttitude.Hostile);
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

                    Context.NonPlayerCharacterManager.SpawnNPC(randomPosition, spawnDef, ENPCSpawnType.Invasion, ETeamID.EnemiesTeamB, EAttitude.Hostile);
                }
                flip = true;
            }
        }

        private NonPlayerCharacterDefinition GetRandomSpawnDefinition()
        {
            if (_debugSpawnDefinitions == null || _debugSpawnDefinitions.Count == 0)
                return null;

            int index = Random.Range(0, _debugSpawnDefinitions.Count);
            return _debugSpawnDefinitions[index];
        }
    }
}