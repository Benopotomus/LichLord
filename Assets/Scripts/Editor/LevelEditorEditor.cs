using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using LichLord.Props;
using LichLord.World;
using UnityEngine.SceneManagement;
using UnityEditorInternal; // Required for InternalEditorUtility

[CustomEditor(typeof(LevelEditor))]
public class LevelEditorEditor : Editor
{
    private bool isPlacing = false;
    private bool useSurfaceNormal = false;
    private Vector3 forwardDirection = Vector3.forward;
    private int maxPropsPerChunk = 512; // Configurable max props per chunk
    private bool blockPlacementOnLimit = false; // Toggle to block or allow placement
    private string warningMessage = ""; // Store warning for Scene View display

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
        LevelEditor manager = (LevelEditor)target;

        if (target == null || manager.MarkerPrefab == null || !isPlacing)
        {
            warningMessage = "";
            return;
        }

        Event e = Event.current;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // Check for mouse click to place marker
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Get chunk coordinates for the hit position
                FChunkPosition chunkCoord = manager.WorldSettings.GetChunkCoordFromPosition(hit.point);
                ChunkMarkupData chunkData = manager.WorldSettings.GetOrCreateMarkupData(chunkCoord);
                int currentPropCount = chunkData.PropMarkupDatas != null ? chunkData.PropMarkupDatas.Length : 0;

                // Check if chunk exceeds prop limit
                if (currentPropCount >= maxPropsPerChunk)
                {
                    warningMessage = $"Warning: Chunk ({chunkCoord.X}, {chunkCoord.Y}) has {currentPropCount}/{maxPropsPerChunk} props. Consider reducing props.";
                    Debug.LogWarning(warningMessage);

                    if (blockPlacementOnLimit)
                    {
                        e.Use();
                        return; // Block placement
                    }
                }
                else
                {
                    warningMessage = ""; // Clear warning if under limit
                }

                Debug.Log(manager.MarkerPrefab);
                LevelEditorMarker instance = (LevelEditorMarker)PrefabUtility.InstantiatePrefab(manager.MarkerPrefab, SceneManager.GetActiveScene());

                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Place PropMarker");
                    instance.transform.position = hit.point;
                    instance.transform.SetParent(manager.transform); // Parent to LevelEditor

                    // Apply rotation
                    Quaternion rotation = Quaternion.LookRotation(
                        (useSurfaceNormal ? hit.normal : forwardDirection).normalized,
                        Vector3.up);

                    // Apply random yaw if enabled
                    if (manager.RandomizeYaw)
                    {
                        float randomYaw = Random.Range(0f, 360f);
                        rotation *= Quaternion.Euler(0f, randomYaw, 0f);
                    }

                    instance.transform.rotation = rotation;

                    // Apply random scale within range
                    float randomScale = Random.Range(manager.RandomScaleRange.x, manager.RandomScaleRange.y);
                    instance.MarkerScale = Vector3.one * randomScale;

                    // Save to markup data
                    PropMarkupData propData = new PropMarkupData
                    {
                        guid = currentPropCount,
                        position = instance.transform.position,
                        rotation = instance.transform.rotation,
                        scale = instance.MarkerScale,
                        propDefinition = (instance as PropMarker)?.definition,
                        propDefinitionId = (instance as PropMarker)?.definition?.TableID ?? 0
                    };

                    // Add to chunk's PropMarkupDatas
                    List<PropMarkupData> propList = chunkData.PropMarkupDatas?.ToList() ?? new List<PropMarkupData>();
                    propList.Add(propData);
                    chunkData.PropMarkupDatas = propList.ToArray();

                    // Mark chunk and world settings as dirty
                    EditorUtility.SetDirty(chunkData);
                    EditorUtility.SetDirty(manager.WorldSettings);
                }

                e.Use();
            }
        }

        // Display warning in Scene View
        if (!string.IsNullOrEmpty(warningMessage))
        {
            Handles.BeginGUI();
            GUIStyle style = new GUIStyle(EditorStyles.helpBox) { fontSize = 12, normal = { textColor = Color.yellow } };
            GUILayout.BeginArea(new Rect(10, 10, 400, 50));
            GUILayout.Label(warningMessage, style);
            GUILayout.EndArea();
            Handles.EndGUI();
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

        useSurfaceNormal = EditorGUILayout.Toggle("Use Surface Normal", useSurfaceNormal);

        maxPropsPerChunk = EditorGUILayout.IntField("Max Props Per Chunk", maxPropsPerChunk);
        blockPlacementOnLimit = EditorGUILayout.Toggle("Block Placement on Limit", blockPlacementOnLimit);

        // Ensure scale range values are positive and max is not less than min
        manager.RandomScaleRange = new Vector2(
            Mathf.Max(0.01f, manager.RandomScaleRange.x),
            Mathf.Max(manager.RandomScaleRange.x, manager.RandomScaleRange.y)
        );
        maxPropsPerChunk = Mathf.Max(1, maxPropsPerChunk); // Ensure at least 1 prop allowed

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

        GUILayout.Space(5);

        if (GUILayout.Button("Snap All Markers to Ground", GUILayout.Height(40)))
        {
            SnapAllMarkersToGround(manager);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Clear All Previews", GUILayout.Height(40)))
        {
            PropPreviewManager.ClearAllPreviewsForAllMarkers();
        }

        // Mark the LevelEditor as dirty to ensure changes are saved
        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }

    private void SnapAllMarkersToGround(LevelEditor manager)
    {
        if (manager.WorldSettings == null)
        {
            Debug.LogWarning("WorldSettings is not assigned.");
            return;
        }

        // Find all LevelEditorMarker instances in the scene
        LevelEditorMarker[] markers = GameObject.FindObjectsOfType<LevelEditorMarker>(true);
        if (markers.Length == 0)
        {
            Debug.Log("No LevelEditorMarkers found in the scene.");
            return;
        }

        Undo.RecordObjects(markers, "Snap Markers to Ground");

        int snappedCount = 0;
        int failedCount = 0;

        // Process each marker
        foreach (var marker in markers)
        {

            snappedCount++;

            // Update PropMarkupData if this is a PropMarker
            if (marker is PropMarker propMarker && propMarker.definition != null)
            {
                SnapToGround(propMarker);
            }
            // Update InvasionSpawnPointMarkupData if this is an InvasionSpawnPointMarker
            else if (marker is InvasionSpawnPointMarker)
            {
                
            }
            else
            {
                failedCount++;
                Debug.LogWarning($"Failed to snap marker at {marker.transform.position} to ground: No ground hit detected.");
            }
        }

        // Mark WorldSettings as dirty
        EditorUtility.SetDirty(manager.WorldSettings);

        // Log results
        Debug.Log($"Snapped {snappedCount} markers to ground. Failed to snap {failedCount} markers.");

        // Force Scene View repaint
        SceneView.RepaintAll();
    }

    private void SnapToGround(PropMarker marker)
    {
        Vector3 origin = marker.transform.position + Vector3.up * 500; // start a bit above
        Vector3 direction = Vector3.down;
        float maxDistance = 1000f;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance);

        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(marker.transform))
                continue;

            Undo.RecordObject(marker.transform, "Snap PropMarker To Ground");
            marker.transform.position = hit.point;
            EditorUtility.SetDirty(marker.transform);
            return;
        }

        Debug.LogWarning($"Snap to Ground: No ground found below PropMarker '{marker.name}' (ignoring self).");
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
        Dictionary<FChunkPosition, int> chunkPropCounts = new(); // Track prop counts per chunk

        foreach (var marker in propMarkers)
        {
            if (marker.definition == null) continue;

            Vector3 position = marker.transform.position;
            Quaternion rotation = marker.transform.rotation;
            Vector3 scale = marker.MarkerScale;
            FChunkPosition chunkCoord = manager.WorldSettings.GetChunkCoordFromPosition(position);

            if (!chunkToProps.TryGetValue(chunkCoord, out var list))
            {
                list = new List<PropMarkupData>();
                chunkToProps[chunkCoord] = list;
                chunkPropCounts[chunkCoord] = 0; // Initialize count
            }

            list.Add(new PropMarkupData
            {
                guid = list.Count,
                position = position,
                rotation = rotation,
                scale = scale,
                propDefinition = marker.definition,
                propDefinitionId = marker.definition.TableID
            });

            // Increment prop count for the chunk
            chunkPropCounts[chunkCoord]++;
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

        // Build message with prop counts for all chunks
        List<string> chunkSummaries = new List<string>();
        List<string> exceededChunks = new List<string>();
        foreach (var kvp in chunkPropCounts)
        {
            string chunkStr = $"Chunk ({kvp.Key.X}, {kvp.Key.Y}) has {kvp.Value} props";
            chunkSummaries.Add(chunkStr);
            if (kvp.Value > maxPropsPerChunk)
            {
                exceededChunks.Add($"Chunk ({kvp.Key.X}, {kvp.Key.Y}) has {kvp.Value}/{maxPropsPerChunk} props");
            }
        }

        // Set warning message with all chunk prop counts and highlight exceeded chunks
        if (chunkSummaries.Count > 0)
        {
            warningMessage = $"Gathered props in chunks: {string.Join(", ", chunkSummaries)}.";
            if (exceededChunks.Count > 0)
            {
                warningMessage += $"\nWarning: The following chunks exceed the prop limit of {maxPropsPerChunk}: {string.Join(", ", exceededChunks)}.";
            }
            Debug.Log(warningMessage);
        }
        else
        {
            warningMessage = "No props gathered.";
            Debug.Log(warningMessage);
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

        // Force Scene View repaint to display warning
        SceneView.RepaintAll();
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

                if (!HasPropMarkerAt(propDefinition, propMarkup.position))
                {
                    var markerDict = editor.PropMarkerPrefabs;
                    if (markerDict.TryGetValue(propDefinition, out PropMarker marker))
                    {
                        LevelEditorMarker instance = (LevelEditorMarker)PrefabUtility.InstantiatePrefab(marker, SceneManager.GetActiveScene());

                        Undo.RegisterCreatedObjectUndo(instance, "Spawn Marker from Chunk Data");

                        instance.transform.position = propMarkup.position;
                        instance.transform.rotation = propMarkup.rotation;
                        instance.MarkerScale = propMarkup.scale; // Apply stored scale
                        instance.transform.SetParent(editor.transform); // Parent to LevelEditor
                    }
                }
            }

            foreach (var invasionMarkup in chunkMarkup.InvasionSpawnPointMarkupDatas)
            {
                if (invasionMarkup == null)
                    continue;

                if (!HasInvasionMarkerAt(invasionMarkup.position))
                {
                    LevelEditorMarker instance = (LevelEditorMarker)PrefabUtility.InstantiatePrefab(editor.InvasionSpawnMarkerPrefab, SceneManager.GetActiveScene());

                    Undo.RegisterCreatedObjectUndo(instance, "Spawn Marker from Chunk Data");

                    instance.transform.position = invasionMarkup.position;
                    instance.transform.rotation = Quaternion.identity;
                    instance.transform.SetParent(editor.transform); // Parent to LevelEditor
                }
            }
        }
    }

    private float markerDetectionRadius = 0.2f;

    private bool HasPropMarkerAt(PropDefinition definition, Vector3 position)
    {
        return GameObject.FindObjectsOfType<PropMarker>()
            .Any(marker =>
                marker.definition == definition &&
                Vector3.Distance(marker.transform.position, position) < markerDetectionRadius);
    }

    private bool HasInvasionMarkerAt(Vector3 position)
    {
        return GameObject.FindObjectsOfType<InvasionSpawnPointMarker>()
            .Any(marker => Vector3.Distance(marker.transform.position, position) < markerDetectionRadius);
    }
}