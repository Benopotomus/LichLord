using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using LichLord.Props;
using LichLord.World;
using LichLord;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(LevelEditor))]
public class LevelEditorEditor : Editor
{
    private GameObject markerPrefab;
    private bool isPlacing = false;
    private bool useSurfaceNormal = false;
    private Vector3 forwardDirection = Vector3.forward;

    static LevelEditorEditor()
    {
        SceneView.duringSceneGui += OnGlobalSceneGUI;
    }

    private static void OnGlobalSceneGUI(SceneView sceneView)
    {
        foreach (var editor in Resources.FindObjectsOfTypeAll<LevelEditorEditor>())
        {
            editor.DrawSceneView(sceneView);
        }
    }

    private void DrawSceneView(SceneView sceneView)
    {
        if (target == null || markerPrefab == null || !isPlacing)
            return;

        LevelEditor manager = (LevelEditor)target;
        Event e = Event.current;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(markerPrefab, SceneManager.GetActiveScene());

                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Place PropMarker");
                    instance.transform.position = hit.point;

                    Quaternion rotation = Quaternion.LookRotation(
                        (useSurfaceNormal ? hit.normal : forwardDirection).normalized,
                        Vector3.up);
                    instance.transform.rotation = rotation;

                    //Selection.activeGameObject = instance;
                }

                e.Use();
            }
        }
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);  // Adds 10 pixels of vertical space

        LevelEditor manager = (LevelEditor)target;

        if (manager.WorldSettings == null)
        {
            EditorGUILayout.HelpBox("Please assign a WorldSettings to the World Settings field.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Marker Settings", EditorStyles.boldLabel);

        markerPrefab = EditorGUILayout.ObjectField("Marker to Spawn", markerPrefab, typeof(GameObject), false) as GameObject;

        useSurfaceNormal = EditorGUILayout.Toggle("Use Surface Normal", useSurfaceNormal);

        if (!useSurfaceNormal)
        {
            forwardDirection = EditorGUILayout.Vector3Field("Forward Direction", forwardDirection.normalized);
            forwardDirection = forwardDirection.normalized;
        }

        GUILayout.Space(5);
        if (GUILayout.Button(isPlacing ? "Stop Placing Markers" : "Start Placing Markers", GUILayout.Height(40)))
        {
            isPlacing = !isPlacing;
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Clear All Markup Data", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog(
                    "Confirm Clear",
                    "Are you sure you want to clear all markup data? This action cannot be undone.",
                    "Yes", "No"))
            {

                Undo.RecordObject(manager.WorldSettings, "Clear Prop Points");
                int clearedCount = 0;
                foreach (ChunkMarkupData markupData in manager.WorldSettings.ChunkMarkupDatas)
                {
                    if (markupData != null)
                    {
                        markupData.PropMarkupDatas = new PropMarkupData[0];
                        EditorUtility.SetDirty(markupData);
                        clearedCount++;
                    }
                }
                EditorUtility.SetDirty(manager.WorldSettings);
                Debug.Log($"Cleared markup data in {clearedCount} chunk(s).");
            }
            else
            {
                Debug.Log("Clear operation canceled by user.");
            }
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Gather All Markers Into World Settings", GUILayout.Height(40)))
        {
            GatherAllMarkersIntoWorldSettings(manager);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Regenerate Markers from Markup Data", GUILayout.Height(40)))
        {
            RegenerateMarkers();
        }
    }

    private void GatherAllMarkersIntoWorldSettings(LevelEditor manager)
    {
        if (manager.WorldSettings == null)
        {
            Debug.LogWarning("WorldSettings is not assigned.");
            return;
        }

        PropMarker[] propMarkers = GameObject.FindObjectsOfType<PropMarker>(true);
        Dictionary<FChunkPosition, List<PropMarkupData>> chunkToProps = new();

        foreach (var marker in propMarkers)
        {
            if (marker.definition == null) continue;

            Vector3 position = marker.transform.position;
            Quaternion rotation = marker.transform.rotation;
            FChunkPosition chunkCoord = manager.WorldSettings.GetChunkCoordFromPosition(position);

            if (!chunkToProps.TryGetValue(chunkCoord, out var list))
            {
                list = new List<PropMarkupData>();
                chunkToProps[chunkCoord] = list;
            }

            list.Add(new PropMarkupData
            {
                guid = list.Count,
                position = position,
                rotation = rotation,
                propDefinition = marker.definition,
                propDefinitionId = marker.definition.TableID
            });
        }

        InvasionSpawnPointMarker[] invasionSpawnMarkers = GameObject.FindObjectsOfType<InvasionSpawnPointMarker>(true);
        Dictionary<FChunkPosition, List<InvasionSpawnPointMarkupData>> chunkToInvasionSpawns = new();

        foreach (var marker in invasionSpawnMarkers)
        {
            Vector3 position = marker.transform.position;
            Quaternion rotation = marker.transform.rotation;
            FChunkPosition chunkCoord = manager.WorldSettings.GetChunkCoordFromPosition(position);

            if (!chunkToInvasionSpawns.TryGetValue(chunkCoord, out var list))
            {
                list = new List<InvasionSpawnPointMarkupData>();
                chunkToInvasionSpawns[chunkCoord] = list;
            }

            list.Add(new InvasionSpawnPointMarkupData
            {
                guid = list.Count,
                position = position,
            });
        }

        Undo.RecordObject(manager.WorldSettings, "Gather PropMarkers");

        foreach (var kvp in chunkToProps)
        {
            FChunkPosition chunk = kvp.Key;
            List<PropMarkupData> newProps = kvp.Value;

            ChunkMarkupData markupData = manager.WorldSettings.GetOrCreateMarkupData(chunk);

            markupData.PropMarkupDatas = newProps.ToArray();

            EditorUtility.SetDirty(markupData);
        }

        foreach (var kvp in chunkToInvasionSpawns)
        {
            FChunkPosition chunk = kvp.Key;
            List<InvasionSpawnPointMarkupData> newInvasions = kvp.Value;

            ChunkMarkupData markupData = manager.WorldSettings.GetOrCreateMarkupData(chunk);

            markupData.InvasionSpawnPointMarkupDatas = newInvasions.ToArray();

            EditorUtility.SetDirty(markupData);
        }

        EditorUtility.SetDirty(manager.WorldSettings);
        Debug.Log($"Gathered {propMarkers.Length} PropMarkers into WorldSettings.");
    }

    private void RegenerateMarkers()
    {
        LevelEditor editor = (LevelEditor)target;
        WorldSettings worldSettings = editor.WorldSettings;

        if (worldSettings == null || worldSettings.ChunkMarkupDatas == null)
        {
            Debug.LogWarning("WorldSettings or ChunkMarkupDatas is missing.");
            return;
        }

        foreach (var chunkMarkup in worldSettings.ChunkMarkupDatas)
        {
            if (chunkMarkup == null || chunkMarkup.PropMarkupDatas == null)
                continue;

            foreach (var propMarkup in chunkMarkup.PropMarkupDatas)
            {
                if (propMarkup == null)
                    continue;

                var propDefinition = editor.GlobalTables.PropTable.TryGetDefinition(propMarkup.propDefinitionId);
                if (propDefinition == null)
                    continue;

                if (!HasMarkerAt(propDefinition, propMarkup.position))
                {
                    GameObject go = new GameObject("PropMarker");
                    Undo.RegisterCreatedObjectUndo(go, "Spawn Marker from Chunk Data");

                    var propMarker = go.AddComponent<PropMarker>();
                    propMarker.definition = propDefinition;

                    go.transform.position = propMarkup.position;
                    go.transform.rotation = Quaternion.identity;
                }
            }
        }
    }

    private float markerDetectionRadius = 0.2f;

    private bool HasMarkerAt(PropDefinition definition, Vector3 position)
    {
        return GameObject.FindObjectsOfType<PropMarker>()
            .Any(marker =>
                marker.definition == definition &&
                Vector3.Distance(marker.transform.position, position) < markerDetectionRadius);
    }
}
