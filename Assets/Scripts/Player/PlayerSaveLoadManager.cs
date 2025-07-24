using UnityEngine;
using System.IO;
using Fusion;
using System;

namespace LichLord
{
    public class PlayerSaveLoadManager : ContextBehaviour
    {
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
                    moveState = pc.Movement.CurrentMoveState
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