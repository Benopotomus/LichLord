using UnityEngine;
using LichLord.Props;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement; // For PrefabStageUtility
#endif

[ExecuteAlways]
public class PropMarker : MonoBehaviour
{
    public PropDefinition definition;

#if UNITY_EDITOR
    private const string PreviewObjectName = "_EditorPreview";
    private GameObject _editorInstance;

    private const float maxDrawRange = 200f;
    private Vector3 _lastCameraPosition;
    private const float cameraMoveThreshold = 0.5f;

    void Awake()
    {
        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        //ScheduleRefreshPreview();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
       // ScheduleDestroyPreview();
    }

    private void OnValidate()
    {
       // ScheduleRefreshPreview();
    }

    private bool _refreshScheduled = false;

    private void ScheduleRefreshPreview()
    {
        if (_refreshScheduled) return;
        _refreshScheduled = true;

        PropMarker self = this;

        EditorApplication.delayCall += () =>
        {
            if (self == null || self.transform == null)
                return;

            _refreshScheduled = false;
            self.DestroyPreview();
            self.CreatePreview();
        };
    }

    private void ScheduleDestroyPreview()
    {
        PropMarker self = this;

        EditorApplication.delayCall += () =>
        {
            if (self == null || self.transform == null)
                return;

            self.DestroyPreview();
        };
    }

    private void CreatePreview()
    {
        if (definition == null || definition.prefab == null)
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
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(definition.prefab);

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

    private void OnSceneGUI(SceneView sceneView)
    {
        if (sceneView.camera == null)
            return;

        Vector3 camPos = sceneView.camera.transform.position;
        float distMoved = Vector3.Distance(camPos, _lastCameraPosition);

        if (distMoved > cameraMoveThreshold)
        {
            _lastCameraPosition = camPos;
            ScheduleRefreshPreview();
        }

        // Make sure preview stays at local zero
        if (_editorInstance != null)
        {
            if (_editorInstance.transform.localPosition != Vector3.zero)
            {
                _editorInstance.transform.localPosition = Vector3.zero;
            }
        }
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
