using UnityEngine;
using LichLord.Props;
using LichLord;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[ExecuteAlways]
public class PropMarker : LevelEditorMarker
{
    public PropDefinition definition;


    private void OnEnable()
    {
        // Register with the PropPreviewManager
        PropPreviewManager.RegisterMarker(this);
    }

    private void OnDisable()
    {
        // Unregister from the PropPreviewManager
        PropPreviewManager.UnregisterMarker(this);
    }

    // Remove OnSceneGUI as it's no longer needed
}
#endif