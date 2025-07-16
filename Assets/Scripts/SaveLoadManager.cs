using AYellowpaper.SerializedCollections;
using UnityEngine;
using System.IO;
using System;
using LichLord.World;

namespace LichLord
{
    public class SaveLoadManager : MonoBehaviour
    {
        public static SaveLoadManager instance;

        [SerializeField]
        [SerializedDictionary("SessionID", "WorldSavedData")]
        private SerializedDictionary<string, string> _worldSavesLoaded;

        [SerializeField]
        [SerializedDictionary("SessionID", "NPCSavedData")]
        private SerializedDictionary<string, string> _npcSavesLoaded;

        [SerializeField]
        [SerializedDictionary("PlayerKey", "PlayerSavedData")]
        private SerializedDictionary<string, string> _playerSavesLoaded;

        private readonly string worldSaveFilePrefix = "WorldSaveData_"; // For world saves
        private readonly string npcSaveFilePrefix = "NPCSaveData_"; // For npc saves
        private readonly string playerSaveFilePrefix = "PlayerSaveData_"; // For player saves

        public void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("Multiple SaveLoadManager instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize dictionaries if null
            if (_worldSavesLoaded == null)
            {
                _worldSavesLoaded = new SerializedDictionary<string, string>();
            }
            if (_playerSavesLoaded == null)
            {
                _playerSavesLoaded = new SerializedDictionary<string, string>();
            }
        }

        public void Start()
        {
            //PopulateSaveData();
        }

        public void PopulateSaveData()
        {
            try
            {
                string directory = Application.persistentDataPath;
                int loadedWorldFiles = 0;
                int loadedPlayerFiles = 0;
                int loadedNPCFiles = 0;

                // World files
                string[] worldFiles = Directory.GetFiles(directory, $"{worldSaveFilePrefix}*.json");
                foreach (string file in worldFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string sessionName = fileName.Substring(worldSaveFilePrefix.Length);

                    if (!string.IsNullOrEmpty(sessionName))
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            if (!string.IsNullOrEmpty(json))
                            {
                                JsonUtility.FromJson<FWorldSaveData>(json); // validate
                                _worldSavesLoaded[sessionName] = json;
                                loadedWorldFiles++;
                            }
                            else
                            {
                                Debug.LogWarning($"Empty JSON in world save file {file}. Skipping.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Invalid JSON in world save file {file}: {ex.Message}. Skipping.");
                        }
                    }
                }

                // Player files
                string[] playerFiles = Directory.GetFiles(directory, $"{playerSaveFilePrefix}*.json");
                foreach (string file in playerFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string playerKey = fileName.Substring(playerSaveFilePrefix.Length);

                    if (!string.IsNullOrEmpty(playerKey))
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            if (!string.IsNullOrEmpty(json))
                            {
                                JsonUtility.FromJson<FPlayerSaveData>(json); // validate
                                _playerSavesLoaded[playerKey] = json;
                                loadedPlayerFiles++;
                            }
                            else
                            {
                                Debug.LogWarning($"Empty JSON in player save file {file}. Skipping.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Invalid JSON in player save file {file}: {ex.Message}. Skipping.");
                        }
                    }
                }

                // NPC files
                string[] npcFiles = Directory.GetFiles(directory, $"{npcSaveFilePrefix}*.json");
                foreach (string file in npcFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string sessionName = fileName.Substring(npcSaveFilePrefix.Length);

                    if (!string.IsNullOrEmpty(sessionName))
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            if (!string.IsNullOrEmpty(json))
                            {
                                JsonUtility.FromJson<FNonPlayerCharacterSaveState>(json); // validate
                                _npcSavesLoaded[sessionName] = json;
                                loadedNPCFiles++;
                            }
                            else
                            {
                                Debug.LogWarning($"Empty JSON in NPC save file {file}. Skipping.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Invalid JSON in NPC save file {file}: {ex.Message}. Skipping.");
                        }
                    }
                }

                Debug.Log($"Populated SaveLoadManager: {loadedWorldFiles} world files, {loadedNPCFiles} NPC files, {loadedPlayerFiles} player files.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to populate save data files: {e.Message}");
            }
        }

        // WORLD
        public bool TryGetWorldData(string sessionName, out string json)
        {
            json = null;
            if (string.IsNullOrEmpty(sessionName))
            {
                sessionName = "default";
            }
            return _worldSavesLoaded.TryGetValue(sessionName, out json);
        }

        public void SetWorldData(string sessionName, string json)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                sessionName = "default";
            }
            _worldSavesLoaded[sessionName] = json;
        }

        public void ClearWorldData(string sessionName = null)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                int clearedCount = _worldSavesLoaded.Count;
                _worldSavesLoaded.Clear();
                Debug.Log($"Cleared {clearedCount} world save data entries from SaveLoadManager.");
            }
            else
            {
                if (_worldSavesLoaded.Remove(sessionName))
                {
                    Debug.Log($"Cleared world save data for session {sessionName} from SaveLoadManager.");
                }
                else
                {
                    Debug.Log($"No world save data found for session {sessionName} in SaveLoadManager.");
                }
            }
        }

        // NPCS
        public bool TryGetNPCData(string sessionName, out string json)
        {
            json = null;
            if (string.IsNullOrEmpty(sessionName))
            {
                sessionName = "default";
            }
            return _npcSavesLoaded.TryGetValue(sessionName, out json);
        }

        public void SetNPCData(string sessionName, string json)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                sessionName = "default";
            }
            _npcSavesLoaded[sessionName] = json;
        }

        public void ClearNPCData(string sessionName = null)
        {
            if (string.IsNullOrEmpty(sessionName))
            {
                int clearedCount = _npcSavesLoaded.Count;
                _npcSavesLoaded.Clear();
                Debug.Log($"Cleared {clearedCount} NPC save data entries from SaveLoadManager.");
            }
            else
            {
                if (_npcSavesLoaded.Remove(sessionName))
                {
                    Debug.Log($"Cleared NPC save data for session {sessionName} from SaveLoadManager.");
                }
                else
                {
                    Debug.Log($"No NPC save data found for session {sessionName} in SaveLoadManager.");
                }
            }
        }

        // PLAYER
        public bool TryGetPlayerData(string playerKey, out string json)
        {

            json = null;
            return false;
            if (string.IsNullOrEmpty(playerKey))
            {
                playerKey = "default";
            }
            return _playerSavesLoaded.TryGetValue(playerKey, out json);
        }

        public void SetPlayerData(string playerKey, string json)
        {
            if (string.IsNullOrEmpty(playerKey))
            {
                playerKey = "default";
            }
            _playerSavesLoaded[playerKey] = json;
        }

        public void ClearPlayerData(string playerKey = null)
        {
            if (string.IsNullOrEmpty(playerKey))
            {
                int clearedCount = _playerSavesLoaded.Count;
                _playerSavesLoaded.Clear();
                Debug.Log($"Cleared {clearedCount} player save data entries from SaveLoadManager.");
            }
            else
            {
                if (_playerSavesLoaded.Remove(playerKey))
                {
                    Debug.Log($"Cleared player save data for player {playerKey} from SaveLoadManager.");
                }
                else
                {
                    Debug.Log($"No player save data found for player {playerKey} in SaveLoadManager.");
                }
            }
        }

        // PATHS
        public string GetWorldSaveFilePath(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("World save key is null or empty; using default key.");
                key = "default";
            }
            // Sanitize key to avoid invalid file path characters
            string sanitizedKey = System.Text.RegularExpressions.Regex.Replace(key, "[^a-zA-Z0-9_-]", "_");
            return Path.Combine(Application.persistentDataPath, $"{worldSaveFilePrefix}{sanitizedKey}.json");
        }

        public string GetPlayerSaveFilePath(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("Player save key is null or empty; using default key.");
                key = "default";
            }
            // Sanitize key to avoid invalid file path characters
            string sanitizedKey = System.Text.RegularExpressions.Regex.Replace(key, "[^a-zA-Z0-9_-]", "_");
            return Path.Combine(Application.persistentDataPath, $"{playerSaveFilePrefix}{sanitizedKey}.json");
        }

        public string GetNPCSaveFilePath(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("NPC save key is null or empty; using default key.");
                key = "default";
            }
            // Sanitize key to avoid invalid file path characters
            string sanitizedKey = System.Text.RegularExpressions.Regex.Replace(key, "[^a-zA-Z0-9_-]", "_");
            return Path.Combine(Application.persistentDataPath, $"{npcSaveFilePrefix}{sanitizedKey}.json");
        }
    }
}