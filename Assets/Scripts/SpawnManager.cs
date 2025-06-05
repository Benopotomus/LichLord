#define ENABLE_LOGS

using System;
using Fusion;
using Starter.Shooter;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LichLord
{
    public class SpawnManager : ContextBehaviour
    {
        [SerializeField] private PlayerCreature _playerPrefab;
        public PlayerCreature LocalPlayer { get; private set; }

        private SpawnPoint[] _spawnPoints;

        public override void Spawned()
        {
            // Find spawn points
            _spawnPoints = FindObjectsOfType<SpawnPoint>();
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogWarning("No SpawnPoint objects found in the scene!");
            }

            // Spawn the local player
            if (Runner.IsPlayerValid(Runner.LocalPlayer))
            {
                SpawnLocalPlayer(Runner.LocalPlayer);
            }
            else
            {
                Debug.LogWarning("LocalPlayer is invalid, cannot spawn player!");
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Clear reference to avoid UI access after despawn
            LocalPlayer = null;
        }

        private void SpawnLocalPlayer(PlayerRef playerRef)
        {
            if (LocalPlayer != null)
            {
                Debug.LogWarning($"Player for {playerRef} is already spawned!");
                return;
            }

            // Select a spawn position
            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;

            // Spawn player with InputAuthority
            var player = Runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, inputAuthority: playerRef);

            Runner.SetPlayerObject(playerRef, player.Object);

            LocalPlayer = player;

            Debug.Log($"Spawned local player for {playerRef}, Nickname: {player.Nickname}, " +
                $"InputAuthority: {player.Object.InputAuthority}, " +
                $"StateAuthority: {player.Object.StateAuthority}, Position: {spawnPosition}");
        }

        private Vector3 GetSpawnPosition()
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                var spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                var randomPositionOffset = Random.insideUnitCircle * spawnPoint.Radius;
                return spawnPoint.transform.position + new Vector3(randomPositionOffset.x, 0f, randomPositionOffset.y);
            }
            Debug.Log("No spawn points available, using default position (0,0,0)");
            return Vector3.zero;
        }

        public void SpawnNPC()
        { 
        
        }

    }
}