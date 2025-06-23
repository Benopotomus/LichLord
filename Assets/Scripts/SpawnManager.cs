#define ENABLE_LOGS

using System;
using Fusion;
using Starter.Shooter;
using UnityEngine;
using Random = UnityEngine.Random;
using System.IO;

namespace LichLord
{
    public class SpawnManager : ContextBehaviour
    {
        [SerializeField] private PlayerCharacter _playerPrefab;
        public PlayerCharacter LocalPlayer { get; private set; }

        private SpawnPoint[] _spawnPoints;

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Clear reference to avoid UI access after despawn
            LocalPlayer = null;
        }

        public void SpawnLocalPlayer(PlayerRef playerRef)
        {
            // Find spawn points
            _spawnPoints = FindObjectsOfType<SpawnPoint>();
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.LogWarning("No SpawnPoint objects found in the scene!");
            }

            // Spawn the local player
            if (!Runner.IsPlayerValid(Runner.LocalPlayer))
            { 
                Debug.LogWarning("LocalPlayer is invalid, cannot spawn player!");
                return;
            }

            // Default spawn position and rotation
            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;
            EMovementState moveState = EMovementState.Walking;

            if (Runner == null || Runner.SessionInfo == null || string.IsNullOrEmpty(Runner.SessionInfo.Name))
            {
                Debug.LogWarning("No active session; cannot check for saved player data.");
            }

            // Generate unique nickname based on project name
            string nickname = GetInstanceId();

            // Check SaveLoadManager for saved player data
            if (SaveLoadManager.instance != null && Runner != null && !string.IsNullOrEmpty(Runner.SessionInfo.Name))
            {
                string worldId = Runner.SessionInfo.Name;
                string playerKey = $"{worldId}_{nickname}"; // Composite key
                if (SaveLoadManager.instance.TryGetPlayerData(playerKey, out string json))
                {
                    try
                    {
                        FPlayerSaveData savedData = JsonUtility.FromJson<FPlayerSaveData>(json);
                        if (!string.IsNullOrEmpty(savedData.playerName))
                        {
                            spawnPosition = savedData.position;
                            spawnRotation = savedData.rotation;
                            moveState = savedData.moveState;
                            //Debug.Log($"Loaded saved position {spawnPosition} and rotation {spawnRotation} for player in world {worldId} with key {playerKey}.");
                        }
                        else
                        {
                            Debug.LogWarning($"Saved player data for key {playerKey} has invalid playerName; using default spawn.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to deserialize player save data for key {playerKey}: {e.Message}; using default spawn.");
                    }
                }
                else
                {
                    Debug.Log($"No saved player data found for key {playerKey}; using default spawn.");
                }
            }
            else
            {
                Debug.LogWarning("SaveLoadManager instance not found or no active session; using default spawn.");
            }

            if (LocalPlayer == null)
            {
                LocalPlayer = Runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, inputAuthority: playerRef);
            }

            Runner.SetPlayerObject(playerRef, LocalPlayer.Object);

            // Spawn player with InputAuthority

            LocalPlayer.ApplySpawnParameters(spawnPosition, spawnRotation, moveState);

            // Set the unique nickname
            LocalPlayer.Nickname = nickname;

            Debug.Log($"Spawned local player for {playerRef}, Nickname: {LocalPlayer.Nickname ?? "Unknown"}, " +
                      $"InputAuthority: {LocalPlayer.Object.InputAuthority}, " +
                      $"StateAuthority: {LocalPlayer.Object.StateAuthority}, Position: {spawnPosition}");
        }

        private string GetInstanceId()
        {
            // Extract project name from Application.dataPath (e.g., "C:/Projects/MyGame_clone_0/Assets")
            string path = Application.dataPath;
            string projectName = Path.GetFileName(Path.GetDirectoryName(path));
            return string.IsNullOrEmpty(projectName) ? "DefaultInstance" : projectName;
        }

        private Vector3 GetSpawnPosition()
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                var spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
                var randomPositionOffset = Random.insideUnitCircle * spawnPoint.Radius;
                return spawnPoint.transform.position + new Vector3(randomPositionOffset.x, 0f, randomPositionOffset.y);
            }
            //Debug.Log("No spawn points available, using default position (0,0,0)");
            return Vector3.zero;
        }
    }
}