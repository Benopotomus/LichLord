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

        [SerializeField]
        private SpawnPoint[] _spawnPoints;
        [SerializeField] private Vector3 _fallbackSpawnPosition = new Vector3(1000, 0, 1000);

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Clear reference to avoid UI access after despawn
            LocalPlayer = null;
        }

        public void SpawnLocalPlayer(PlayerRef playerRef)
        {
            _spawnPoints = FindObjectsOfType<SpawnPoint>();

            if(LocalPlayer != null) 
                return;

            if (!Runner.IsPlayerValid(Runner.LocalPlayer))
            {
                Debug.LogWarning("LocalPlayer is invalid, cannot spawn player!");
                return;
            }

            FPlayerSaveData loadedPlayerData = Context.PlayerSaveLoadManager.LoadedPlayerSave;

            if (loadedPlayerData.IsValid())
            {
                SpawnPlayerFromSave(playerRef, loadedPlayerData);
            }
            else
            { 
                CreateAndSpawnPlayer(playerRef);
            }

        }

        private void SpawnPlayerFromSave(PlayerRef playerRef, FPlayerSaveData loadedPlayerData)
        {
            Vector3 spawnPosition = loadedPlayerData.position;
            Quaternion spawnRotation = loadedPlayerData.rotation;
            EMovementState moveState = loadedPlayerData.moveState;
            string nickname = loadedPlayerData.playerName;

            LocalPlayer = Runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, inputAuthority: playerRef);
            LocalPlayer.ApplySpawnParameters(spawnPosition, spawnRotation, moveState, nickname);

            Debug.Log($"Spawned local player from save at {spawnPosition} with Nickname {LocalPlayer.Nickname}");
        }

        private void CreateAndSpawnPlayer(PlayerRef playerRef)
        {
            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = Quaternion.identity;
            EMovementState moveState = EMovementState.Walking;
            string nickname = GetInstanceId();

            LocalPlayer = Runner.Spawn(_playerPrefab, spawnPosition, spawnRotation, inputAuthority: playerRef);
            LocalPlayer.ApplySpawnParameters(spawnPosition, spawnRotation, moveState, nickname);

            Debug.Log($"Create local player at {spawnPosition} with Nickname {LocalPlayer.Nickname}");
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
            return _fallbackSpawnPosition;
        }
    }
}