using UnityEngine;
using LichLord.Props;
using LichLord;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement; // For PrefabStageUtility
#endif

[ExecuteAlways]
public class InvasionSpawnPointMarker : LevelEditorMarker
{
    public InvasionSpawnPointDefinition definition;

#if UNITY_EDITOR
    private const string PreviewObjectName = "_EditorPreview";
    private GameObject _editorInstance;

    private const float maxDrawRange = 200f;

    private void CreatePreview()
    {
        if (definition == null || definition.Prefab == null)
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (IsDraggingPrefab() || IsEditingPrefabStage())
            return;

        Camera sceneCamera = SceneView.lastActiveSceneView?.camera;
        if (sceneCamera == null)
            return;

        float dist = Vector3.Distance(sceneCamera.transform.position, transform.position);

        if (dist > maxDrawRange)
        {
            DestroyPreview();
            return;
        }

        Transform existing = transform.Find(PreviewObjectName);
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        // Instantiate prefab without a parent first
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(definition.Prefab);

        if (instance == null)
            return;

        // Now safely parent if this is a scene instance (not prefab asset)
        if (gameObject.scene.IsValid())
        {
            instance.transform.SetParent(transform, false);
        }

        instance.name = PreviewObjectName;

        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        _editorInstance = instance;

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private void DestroyPreview()
    {
        if (transform == null)
            return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == PreviewObjectName)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        _editorInstance = null;
    }

    private bool IsDraggingPrefab()
    {
        if (Event.current != null)
        {
            if (Event.current.type == EventType.MouseDrag)
                return true;

            if (Event.current.type == EventType.MouseDown)
                return true;
        }
        return false;
    }

    private bool IsEditingPrefabStage()
    {
        var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
        return prefabStage != null;
    }
#endif
}
