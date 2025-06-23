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
        [SerializedDictionary("WorldID", "WorldSavedData")]
        private SerializedDictionary<string, string> _worldSavesLoaded;

        [SerializeField]
        [SerializedDictionary("PlayerKey", "PlayerSavedData")]
        private SerializedDictionary<string, string> _playerSavesLoaded;

        private readonly string worldSaveFilePrefix = "WorldSaveData_"; // For world saves
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
            PopulateSaveData();
        }

        public void PopulateSaveData()
        {
            try
            {
                string directory = Application.persistentDataPath;
                int loadedWorldFiles = 0;
                int loadedPlayerFiles = 0;

                // Load world save data files
                string[] worldFiles = Directory.GetFiles(directory, $"{worldSaveFilePrefix}*.json");
                foreach (string file in worldFiles)
                {
                    // Extract session name from file name
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string sessionName = fileName.Substring(worldSaveFilePrefix.Length);

                    if (!string.IsNullOrEmpty(sessionName))
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            // Validate JSON
                            if (!string.IsNullOrEmpty(json))
                            {
                                JsonUtility.FromJson<FWorldSaveData>(json); // Throws if invalid
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

                // Load player save data files
                string[] playerFiles = Directory.GetFiles(directory, $"{playerSaveFilePrefix}*.json");
                foreach (string file in playerFiles)
                {
                    // Extract player key from file name
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string playerKey = fileName.Substring(playerSaveFilePrefix.Length);

                    if (!string.IsNullOrEmpty(playerKey))
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            // Validate JSON
                            if (!string.IsNullOrEmpty(json))
                            {
                                JsonUtility.FromJson<FPlayerSaveData>(json); // Throws if invalid
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

                Debug.Log($"Populated SaveLoadManager: {loadedWorldFiles} world save data files and {loadedPlayerFiles} player save data files.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to populate save data files: {e.Message}");
            }
        }

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

        public bool TryGetPlayerData(string playerKey, out string json)
        {
            json = null;
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
    }
}