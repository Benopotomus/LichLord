#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using LichLord.Props;

[InitializeOnLoad]
public static class PropPreviewManager
{
    private static readonly List<PropMarker> markers = new List<PropMarker>();
    private static readonly Dictionary<PropMarker, GameObject> previewInstances = new Dictionary<PropMarker, GameObject>();
    private static readonly Dictionary<GameObject, Stack<GameObject>> prefabPool = new Dictionary<GameObject, Stack<GameObject>>();
    private static Vector3 lastCameraPosition;
    private static readonly float maxDrawRange = 500f;
    private static readonly float cameraMoveThreshold = 0.5f;
    private static readonly string previewObjectName = "_EditorPreview";
    private static bool isProcessing = false;

    static PropPreviewManager()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    public static void RegisterMarker(PropMarker marker)
    {
        if (marker == null || markers.Contains(marker))
            return;

        // Clean up any existing previews for this marker
        ClearPreviewsForMarker(marker);
        markers.Add(marker);
    }

    public static void UnregisterMarker(PropMarker marker)
    {
        if (marker == null)
            return;

        markers.Remove(marker);
        ClearPreviewsForMarker(marker);
    }

    public static void ClearAllPreviewsForAllMarkers()
    {
        // Clear all previews in the hierarchy for registered markers
        foreach (var marker in markers)
        {
            if (marker != null)
            {
                ClearPreviewsForMarker(marker);
            }
        }

        // Clear any remaining previews in the scene
        var allPreviews = Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allPreviews)
        {
            if (obj != null)
            {
                if (obj.name == previewObjectName && obj.transform.parent != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        // Clear internal tracking
        previewInstances.Clear();
        prefabPool.Clear();
        Debug.Log("Cleared all preview objects and reset preview manager state.");
    }

    private static void ClearPreviewsForMarker(PropMarker marker)
    {
        if (marker == null || marker.transform == null)
            return;

        // Remove any existing preview instances in the hierarchy
        for (int i = marker.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = marker.transform.GetChild(i);
            if (child.name == previewObjectName)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // Remove from previewInstances
        if (previewInstances.ContainsKey(marker))
        {
            previewInstances.Remove(marker);
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (isProcessing || EditorApplication.isPlayingOrWillChangePlaymode || sceneView.camera == null)
            return;

        isProcessing = true;

        try
        {
            Vector3 camPos = sceneView.camera.transform.position;
            float distMoved = Vector3.Distance(camPos, lastCameraPosition);

            if (distMoved > cameraMoveThreshold)
            {
                lastCameraPosition = camPos;
                UpdatePreviews(sceneView.camera);
            }
        }
        finally
        {
            isProcessing = false;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingEditMode)
        {
            ClearAllPreviewsForAllMarkers();
        }
    }

    private static void UpdatePreviews(Camera camera)
    {
        // Create a copy to avoid collection modification issues
        var markersToProcess = new List<PropMarker>(markers);

        foreach (var marker in markersToProcess)
        {
            if (marker == null || marker.definition == null || marker.definition.prefab == null || !marker.gameObject.scene.IsValid())
            {
                ClearPreviewsForMarker(marker);
                markers.Remove(marker);
                continue;
            }

            float sqrDist = Vector3.SqrMagnitude(camera.transform.position - marker.transform.position);
            bool shouldShow = sqrDist <= maxDrawRange * maxDrawRange;

            // Check if preview exists
            bool hasPreview = previewInstances.ContainsKey(marker) && previewInstances[marker] != null;

            if (shouldShow && !hasPreview)
            {
                CreatePreview(marker);
            }
            else if (!shouldShow && hasPreview)
            {
                ClearPreviewsForMarker(marker);
            }
            else if (hasPreview)
            {
                // Update existing preview's transform
                GameObject instance = previewInstances[marker];
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = marker.MarkerScale;
            }
        }
    }

    private static void CreatePreview(PropMarker marker)
    {
        // Ensure no duplicate preview exists in the hierarchy
        ClearPreviewsForMarker(marker);

        GameObject prefab = marker.definition.prefab;
        GameObject instance = null;

        // Try to get from pool
        if (prefabPool.TryGetValue(prefab, out Stack<GameObject> pool) && pool.Count > 0)
        {
            instance = pool.Pop();
            instance.SetActive(true);
        }
        else
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
                return;
        }

        instance.transform.SetParent(marker.transform, false);
        instance.name = previewObjectName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = marker.MarkerScale;

        previewInstances[marker] = instance;
        EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
    }
}
#endif