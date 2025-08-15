using UnityEditor;
using UnityEngine;
using LichLord;
using System.IO;
using System;

[CustomEditor(typeof(SaveLoadManager))]
public class SaveLoadManagerEditor : Editor
{
    private string key = ""; // Text field for session/player ID

    public override void OnInspectorGUI()
    {
        // Draw the default Inspector properties
        DrawDefaultInspector();

        // Check if SaveLoadManager instance exists in the scene
        SaveLoadManager saveLoadManager = FindObjectOfType<SaveLoadManager>();
        if (saveLoadManager == null)
        {
            EditorGUILayout.HelpBox("No SaveLoadManager instance found in the scene. Create one to use these buttons.", MessageType.Warning);
            if (GUILayout.Button("Create SaveLoadManager"))
            {
                try
                {
                    GameObject managerObj = new GameObject("SaveLoadManager");
                    managerObj.AddComponent<SaveLoadManager>();
                    Undo.RegisterCreatedObjectUndo(managerObj, "Create SaveLoadManager");
                    Debug.Log("Created SaveLoadManager GameObject in the scene.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to create SaveLoadManager: {e.Message}");
                }
            }
            return;
        }

        // Add a text field for the session/player ID
        EditorGUILayout.LabelField("Session/Player Key");
        key = EditorGUILayout.TextField(key);

        // Add a button to populate save data
        if (GUILayout.Button("Populate Save Data"))
        {
            try
            {
                saveLoadManager.PopulateSaveData();
                // Mark the SaveLoadManager as dirty to ensure Inspector updates
                EditorUtility.SetDirty(saveLoadManager);
                Debug.Log("Successfully populated save data via Editor button.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to populate save data via Editor button: {e.Message}");
            }
        }

        // Add a button to delete world save file
        if (GUILayout.Button("Delete World Save"))
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogWarning("Session name is empty. Please enter a valid session name.");
                    return;
                }

                // Delete from SaveLoadManager and file system
                string saveFilePath = saveLoadManager.GetWorldSaveFilePath(key);
                saveLoadManager.ClearWorldData(key);

                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                    Debug.Log($"Successfully deleted world save file for session {key} at {saveFilePath} via Editor button.");
                }
                else
                {
                    Debug.Log($"No world save file found for session {key} at {saveFilePath}. Removed from SaveLoadManager if present.");
                }

                // Mark the SaveLoadManager as dirty to ensure Inspector updates
                EditorUtility.SetDirty(saveLoadManager);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete world save file for session {key} via Editor button: {e.Message}");
            }
        }

        // Add a button to delete NPC save file
        if (GUILayout.Button("Delete NPC Save"))
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogWarning("NPC key is empty. Please enter a valid NPC key (e.g., sessionName).");
                    return;
                }

                // Delete from SaveLoadManager and file system
                string saveFilePath = saveLoadManager.GetNPCSaveFilePath(key);
                saveLoadManager.ClearNPCData(key);

                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                    Debug.Log($"Successfully deleted NPC save file for key {key} at {saveFilePath} via Editor button.");
                }
                else
                {
                    Debug.Log($"No NPC save file found for key {key} at {saveFilePath}. Removed from SaveLoadManager if present.");
                }

                // Mark the SaveLoadManager as dirty to ensure Inspector updates
                EditorUtility.SetDirty(saveLoadManager);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete NPC save file for key {key} via Editor button: {e.Message}");
            }
        }

        // Add a button to delete player save file
        if (GUILayout.Button("Delete Player Save"))
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogWarning("Player key is empty. Please enter a valid player key (e.g., sessionName_instanceId).");
                    return;
                }

                // Delete from SaveLoadManager and file system
                string saveFilePath = saveLoadManager.GetPlayerSaveFilePath(key);
                saveLoadManager.ClearPlayerData(key);

                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                    Debug.Log($"Successfully deleted player save file for key {key} at {saveFilePath} via Editor button.");
                }
                else
                {
                    Debug.Log($"No player save file found for key {key} at {saveFilePath}. Removed from SaveLoadManager if present.");
                }

                // Mark the SaveLoadManager as dirty to ensure Inspector updates
                EditorUtility.SetDirty(saveLoadManager);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete player save file for key {key} via Editor button: {e.Message}");
            }
        }

        if (GUILayout.Button("Clear ALL Saves"))
        {
            try
            {
                saveLoadManager.ClearAllData();
                EditorUtility.SetDirty(saveLoadManager);
                Debug.Log("All saves cleared from memory and disk via Editor button.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to clear all saves via Editor button: {e.Message}");
            }
        }
    }
}