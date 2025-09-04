using UnityEngine;
using System.IO;
using Fusion;
using System;

namespace LichLord
{
    public class PlayerSaveLoadManager : ContextBehaviour
    {
        private FPlayerSaveData _loadedPlayerSave = new FPlayerSaveData();
        public FPlayerSaveData LoadedPlayerSave => _loadedPlayerSave;

        public void LoadPlayer()
        {
            // Reset default spawn values in case load fails
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;
            EMovementState moveState = EMovementState.Walking;

            string nickname = GetInstanceId();
            string sessionName = Global.Networking.SessionName;
            string playerKey = $"{sessionName}_{nickname}";

            if (!SaveLoadManager.instance.TryGetPlayerData(playerKey, out string json))
                return;

            try
            {
                FPlayerSaveData savedData = JsonUtility.FromJson<FPlayerSaveData>(json);
                if (!string.IsNullOrEmpty(savedData.playerName))
                {
                    // Fill out struct and public access
                    _loadedPlayerSave = savedData;

                    // Convenience values for spawning
                    spawnPosition = savedData.position;
                    spawnRotation = savedData.rotation;
                    moveState = savedData.moveState;

                    Debug.Log($"Loaded player data for key {playerKey}: Pos={spawnPosition}, Rot={spawnRotation}");
                    return;
                }
                else
                {
                    Debug.LogWarning($"Saved data for key {playerKey} is invalid; using default spawn.");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse saved data for key {playerKey}: {e.Message}");
                return;
            }
        }


        public void SavePlayer(PlayerCharacter pc)
        {
            if (pc == null)
            {
                Debug.LogWarning("PlayerCharacter is null; cannot save player data.");
                return;
            }

            if (Runner == null)
            {
                Debug.LogWarning("No active session; cannot save player data.");
                return;
            }

            if (SaveLoadManager.instance == null)
            {
                Debug.LogError("SaveLoadManager instance not found. Ensure SaveLoadManager is in the scene.");
                return;
            }

            try
            {
                string worldId = Global.Networking.SessionName;
                string instanceId = GetInstanceId();
                string playerKey = $"{worldId}_{instanceId}"; // Composite key

                FPlayerSaveData playerSaveData = new FPlayerSaveData
                {
                    position = pc.transform.position,
                    rotation = pc.transform.rotation,
                    playerName = pc.Nickname ?? "Unknown", // Fallback if Nickname is null
                    moveState = pc.Movement.CurrentMoveState,
                    tutorialProgress = Context.MissionManager.TutorialProgress
                };

                Debug.Log("Saving Position: " + playerSaveData.position);

                // Serialize to JSON
                string json = JsonUtility.ToJson(playerSaveData, true);

                // Store in SaveLoadManager and file system
                SaveLoadManager.instance.SetPlayerData(playerKey, json);

                string saveFilePath = SaveLoadManager.instance.GetPlayerSaveFilePath(playerKey);
                File.WriteAllText(saveFilePath, json);
                Debug.Log($"Saved player data for {pc.Nickname} in world {worldId} with key {playerKey} to {saveFilePath}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save player data for world {Runner?.SessionInfo?.Name}: {e.Message}");
            }
        }

        private string GetInstanceId()
        {
            // Extract project name from Application.dataPath (e.g., "C:/Projects/MyGame_clone_0/Assets")
            string path = Application.dataPath;
            string projectName = Path.GetFileName(Path.GetDirectoryName(path));
            return string.IsNullOrEmpty(projectName) ? "DefaultInstance" : projectName;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            if (PlayerCharacter.TryGetLocalPlayer(Runner, out PlayerCharacter pc))
            {
                SavePlayer(pc);
            }
        }
    }
}