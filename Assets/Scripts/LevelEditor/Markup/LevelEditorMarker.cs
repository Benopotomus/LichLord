using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
#endif

[ExecuteAlways]
public class LevelEditorMarker : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
    }

    protected virtual void OnSceneGUI(SceneView sceneView)
    {
        // Implement drawing or handles here
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            Destroy(gameObject); // Also remove at runtime in editor
        }
    }
#endif
}
