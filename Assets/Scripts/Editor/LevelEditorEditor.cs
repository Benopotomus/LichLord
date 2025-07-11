using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using LichLord.Props;
using LichLord.World;
using System.Linq;

[CustomEditor(typeof(LevelEditor))]
public class LevelEditorEditor : Editor
{
    private PropDefinition newPropDefinition;
    private bool isAddingPoints = false;
    private bool isDeletingPoints = false;
    private Vector3 forwardDirection = Vector3.forward;
    private bool useSurfaceNormal = false;
    private float deleteRadius = 1.0f;

    public float maxDrawDistance = 200f; // Set your desired max draw distance here

    private List<GameObject> _previewInstances = new();

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        CleanupPreviews();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelEditor manager = (LevelEditor)target;

        if (manager.WorldSettings == null)
        {
            EditorGUILayout.HelpBox("Please assign a WorldSettings to the World Settings field.", MessageType.Warning);
            return;
        }

        newPropDefinition = EditorGUILayout.ObjectField("Prop Definition", newPropDefinition, typeof(PropDefinition), false) as PropDefinition;
        useSurfaceNormal = EditorGUILayout.Toggle("Use Surface Normal", useSurfaceNormal);

        if (!useSurfaceNormal)
        {
            forwardDirection = EditorGUILayout.Vector3Field("Forward Direction", forwardDirection.normalized);
            forwardDirection = forwardDirection.normalized;
        }

        if (GUILayout.Button(isAddingPoints ? "Stop Adding Points" : "Start Adding Points"))
        {
            isAddingPoints = !isAddingPoints;
            isDeletingPoints = false;
        }

        if (GUILayout.Button(isDeletingPoints ? "Stop Deleting Points" : "Start Deleting Points"))
        {
            isDeletingPoints = !isDeletingPoints;
            isAddingPoints = false;
        }

        if (isDeletingPoints)
        {
            deleteRadius = EditorGUILayout.FloatField("Delete Radius", deleteRadius);
            deleteRadius = Mathf.Max(0.1f, deleteRadius);
        }

        if (GUILayout.Button("Clear All Points"))
        {
            Undo.RecordObject(manager.WorldSettings, "Clear Prop Points");
            foreach (ChunkPropsMarkupData markupData in manager.WorldSettings.PropMarkupDatas)
            {
                if (markupData != null)
                {
                    markupData.propMarkupDatas = new PropMarkupData[0];
                    EditorUtility.SetDirty(markupData);
                }
            }
            EditorUtility.SetDirty(manager.WorldSettings);
        }

        if (GUILayout.Button("Clean Up World Settings"))
        {
            Undo.RecordObject(manager.WorldSettings, "Clean Up World Settings");
            CleanUpWorldSettings(manager.WorldSettings);
            EditorUtility.SetDirty(manager.WorldSettings);
            EditorUtility.DisplayDialog("Success", "World Settings cleaned up. Check console for details.", "OK");
        }

        if (GUILayout.Button(new GUIContent("Clear Saves", "Deletes the PropSaveData.json file.")))
        {
            string saveFileName = "PropSaveData.json";
            string saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);

            if (File.Exists(saveFilePath))
            {
                try
                {
                    File.Delete(saveFilePath);
                    EditorUtility.DisplayDialog("Success", "PropSaveData.json has been deleted.", "OK");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to delete PropSaveData.json: {e.Message}", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "PropSaveData.json does not exist.", "OK");
            }
        }

        if (GUILayout.Button(new GUIContent("Remove All Markup Data", "Removes all ChunkPropsMarkupData ScriptableObjects from WorldSettings and deletes their sub-assets.")))
        {
            if (EditorUtility.DisplayDialog("Confirm Removal", "Are you sure you want to remove all ChunkPropsMarkupData ScriptableObjects? This will delete their sub-assets and cannot be undone without asset recovery.", "Yes", "No"))
            {
                manager.WorldSettings.RemoveAllMarkupData();
                EditorUtility.DisplayDialog("Success", "ChunkPropsMarkupData removal complete. Check console for details.", "OK");
            }
        }

        if (GUILayout.Button("Import Trees From Terrain"))
        {
            ImportTreesFromTerrain(manager);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        LevelEditor manager = (LevelEditor)target;
        Event e = Event.current;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        CleanupPreviews();

        if (manager.WorldSettings?.PropMarkupDatas != null)
        {
            Camera sceneCam = SceneView.currentDrawingSceneView.camera;


            foreach (var markupData in manager.WorldSettings.PropMarkupDatas)
            {
                if (markupData?.propMarkupDatas == null) continue;

                foreach (var point in markupData.propMarkupDatas)
                {
                    if (point?.propDefinition?.prefab == null) continue;

                    float distance = Vector3.Distance(sceneCam.transform.position, point.position);
                    if (distance > maxDrawDistance)
                        continue;

                    GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(point.propDefinition.prefab);
                    if (preview == null) continue;

                    preview.transform.position = point.position;
                    preview.transform.rotation = point.rotation;
                    preview.transform.localScale = point.propDefinition.prefab.transform.localScale;
                    preview.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
                    _previewInstances.Add(preview);
                }
            }
        }


        if (isAddingPoints && newPropDefinition != null)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    FChunkPosition chunkCoord = manager.WorldSettings.GetChunkCoordFromPosition(hit.point);
                    ChunkPropsMarkupData markupData = manager.WorldSettings.GetOrCreateMarkupData(chunkCoord);

                    Undo.RecordObject(markupData, "Add Prop Point");

                    List<PropMarkupData> points = markupData.propMarkupDatas != null ? new List<PropMarkupData>(markupData.propMarkupDatas) : new List<PropMarkupData>();
                    int guid = GetUniqueGuid(markupData);

                    points.Add(new PropMarkupData
                    {
                        guid = guid,
                        position = hit.point,
                        rotation = Quaternion.LookRotation((useSurfaceNormal ? hit.normal : forwardDirection).normalized, Vector3.up),
                        propDefinition = newPropDefinition
                    });

                    markupData.propMarkupDatas = points.ToArray();
                    EditorUtility.SetDirty(markupData);
                    EditorUtility.SetDirty(manager.WorldSettings);

                    e.Use();
                }
            }

            // Hover Preview
            if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit previewHit))
            {
                GameObject prefab = newPropDefinition.prefab;
                if (prefab != null)
                {
                    GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    preview.transform.position = previewHit.point;
                    Quaternion rotation = Quaternion.LookRotation((useSurfaceNormal ? previewHit.normal : forwardDirection).normalized, Vector3.up);
                    preview.transform.rotation = rotation;
                    preview.transform.localScale = prefab.transform.localScale;
                    preview.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
                    _previewInstances.Add(preview);
                }
            }
        }
        else if (isDeletingPoints)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    FChunkPosition chunkCoord = manager.WorldSettings.GetChunkCoordFromPosition(hit.point);
                    ChunkPropsMarkupData markupData = manager.WorldSettings.GetMarkupData(chunkCoord);

                    if (markupData != null && markupData.propMarkupDatas != null)
                    {
                        List<PropMarkupData> points = new List<PropMarkupData>(markupData.propMarkupDatas);
                        PropMarkupData closestPoint = null;
                        float minDistance = float.MaxValue;

                        foreach (PropMarkupData point in points)
                        {
                            float distance = Vector3.Distance(hit.point, point.position);
                            if (distance < deleteRadius && distance < minDistance)
                            {
                                minDistance = distance;
                                closestPoint = point;
                            }
                        }

                        if (closestPoint != null)
                        {
                            Undo.RecordObject(markupData, "Delete Prop Point");
                            points.Remove(closestPoint);
                            markupData.propMarkupDatas = points.ToArray();
                            EditorUtility.SetDirty(markupData);
                            EditorUtility.SetDirty(manager.WorldSettings);
                        }
                    }

                    e.Use();
                }
            }

            // Draw delete disc
            if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit previewHit))
            {
                Handles.color = new Color(1f, 0f, 0f, 0.5f);
                Handles.DrawWireDisc(previewHit.point, previewHit.normal, deleteRadius);
            }
        }

        sceneView.Repaint();
    }

    private void CleanupPreviews()
    {
        foreach (var obj in _previewInstances)
        {
            if (obj != null)
            {
                GameObject.DestroyImmediate(obj);
            }
        }
        _previewInstances.Clear();
    }

    private int GenerateUniqueGuid(HashSet<int> usedGuids)
    {
        int newGuid = usedGuids.Count > 0 ? 0 : 1;
        if (newGuid == 0)
        {
            newGuid = usedGuids.Max() + 1;
        }

        while (usedGuids.Contains(newGuid))
            newGuid++;

        return newGuid;
    }

    private int GetUniqueGuid(ChunkPropsMarkupData markupData)
    {
        HashSet<int> usedGuids = new HashSet<int>();
        if (markupData.propMarkupDatas != null)
        {
            for (int i = 0; i < markupData.propMarkupDatas.Length; i++)
            {
                usedGuids.Add(markupData.propMarkupDatas[i].guid);
            }
        }

        int newGuid = 1;
        while (usedGuids.Contains(newGuid))
            newGuid++;

        return newGuid;
    }

    private void CleanUpWorldSettings(WorldSettings worldSettings)
    {
        if (worldSettings == null || worldSettings.PropMarkupDatas == null)
            return;

        List<ChunkPropsMarkupData> validMarkupDatas = new List<ChunkPropsMarkupData>();
        for (int i = 0; i < worldSettings.PropMarkupDatas.Count; i++)
        {
            ChunkPropsMarkupData markupData = worldSettings.PropMarkupDatas[i];
            if (markupData == null)
                continue;

            List<PropMarkupData> points = new List<PropMarkupData>();
            if (markupData.propMarkupDatas != null)
            {
                for (int j = 0; j < markupData.propMarkupDatas.Length; j++)
                {
                    PropMarkupData p = markupData.propMarkupDatas[j];
                    if (p != null && p.propDefinition != null)
                        points.Add(p);
                }
            }

            HashSet<int> usedGuids = new HashSet<int>();
            for (int k = 0; k < points.Count; k++)
            {
                while (points[k].guid == 0 || usedGuids.Contains(points[k].guid))
                {
                    points[k].guid = GenerateUniqueGuid(usedGuids);
                }
                usedGuids.Add(points[k].guid);
            }

            markupData.propMarkupDatas = points.ToArray();
            if (points.Count > 0)
                validMarkupDatas.Add(markupData);

            EditorUtility.SetDirty(markupData);
        }

        worldSettings.PropMarkupDatas.Clear();
        worldSettings.PropMarkupDatas.AddRange(validMarkupDatas);
        EditorUtility.SetDirty(worldSettings);
    }

    private void ImportTreesFromTerrain(LevelEditor manager)
    {
        if (manager.WorldSettings == null)
        {
            Debug.LogWarning("WorldSettings is null.");
            return;
        }

        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0)
        {
            Debug.LogWarning("No active terrains found in the scene.");
            return;
        }

        int addedCount = 0;

        for (int t = 0; t < terrains.Length; t++)
        {
            Terrain terrain = terrains[t];
            TerrainID idComponent = terrain.GetComponent<TerrainID>();
            if (idComponent == null)
            {
                Debug.LogWarning($"Terrain '{terrain.name}' is missing a TerrainID component.");
                continue;
            }

            string terrainID = idComponent.ID;
            Vector3 terrainPos = terrain.transform.position;
            TerrainData terrainData = terrain.terrainData;

            TreePrototype[] prototypes = terrainData.treePrototypes;
            TreeInstance[] trees = terrainData.treeInstances;

            for (int i = 0; i < trees.Length; i++)
            {
                TreeInstance tree = trees[i];
                Vector3 worldPos = Vector3.Scale(tree.position, terrainData.size) + terrainPos;
                float yRotation = tree.rotation * 360f;
                Quaternion rotation = Quaternion.Euler(0, yRotation, 0);

                int prototypeIndex = tree.prototypeIndex;
                if (prototypeIndex < 0 || prototypeIndex >= prototypes.Length)
                    continue;

                GameObject prefab = prototypes[prototypeIndex].prefab;
                if (prefab == null)
                    continue;

                PropDefinition def = FindPropDefinitionByPrefab(prefab);
                if (def == null)
                    continue;

                FChunkPosition chunkCoord = manager.WorldSettings.GetChunkCoordFromPosition(worldPos);
                ChunkPropsMarkupData markupData = manager.WorldSettings.GetOrCreateMarkupData(chunkCoord);

                PropMarkupData[] existing = markupData.propMarkupDatas ?? new PropMarkupData[0];

                bool alreadyExists = false;
                for (int j = 0; j < existing.Length; j++)
                {
                    if (Vector3.Distance(existing[j].position, worldPos) < 0.1f && existing[j].propDefinition == def)
                    {
                        alreadyExists = true;
                        break;
                    }
                }
                if (alreadyExists)
                    continue;

                Undo.RecordObject(markupData, "Import Tree");
                Undo.RecordObject(manager.WorldSettings, "Import Tree");

                PropMarkupData[] updated = new PropMarkupData[existing.Length + 1];
                for (int j = 0; j < existing.Length; j++)
                    updated[j] = existing[j];

                int guid = GetUniqueGuid(markupData);

                updated[existing.Length] = new PropMarkupData
                {
                    guid = guid,
                    position = worldPos,
                    rotation = rotation,
                    propDefinition = def,
                    terrainId = terrainID,
                    terrainTreePosition = worldPos
                };

                markupData.propMarkupDatas = updated;
                EditorUtility.SetDirty(markupData);
                EditorUtility.SetDirty(manager.WorldSettings);
                addedCount++;
            }
        }

        Debug.Log($"Imported {addedCount} trees into prop markups.");
    }

    private PropDefinition FindPropDefinitionByPrefab(GameObject prefab)
    {
        return newPropDefinition;

        // TODO: Replace this with your actual lookup method
        PropDefinition[] allDefs = Resources.FindObjectsOfTypeAll<PropDefinition>();
        for (int i = 0; i < allDefs.Length; i++)
        {
            if (allDefs[i].prefab == prefab)
                return allDefs[i];
        }
        return null;
    }
}
