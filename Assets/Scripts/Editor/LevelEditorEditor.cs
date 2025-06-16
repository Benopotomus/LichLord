using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using LichLord.Props;
using System.IO;
using System.Linq;
using LichLord.World;

[CustomEditor(typeof(LevelEditor))]
public class LevelEditorEditor : Editor
{
    private PropDefinition newPropDefinition;
    private bool isAddingPoints = false;
    private Vector3 forwardDirection = Vector3.forward;
    private bool useSurfaceNormal = false;

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
            if (isAddingPoints)
            {
                Selection.activeGameObject = manager.gameObject;
                SceneView.duringSceneGui += OnSceneGUI;
            }
            else
            {
                SceneView.duringSceneGui -= OnSceneGUI;
            }
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
    }

    private void CleanUpWorldSettings(WorldSettings worldSettings)
    {
        if (worldSettings == null || worldSettings.PropMarkupDatas == null)
        {
            Debug.LogWarning("WorldSettings or PropMarkupDatas is null, cannot clean up.");
            return;
        }

        // Track all GUIDs to detect duplicates
        HashSet<int> usedGuids = new HashSet<int>();
        List<ChunkPropsMarkupData> validMarkupDatas = new List<ChunkPropsMarkupData>();
        int removedMarkupDatas = 0;
        int removedProps = 0;
        int reassignedGuids = 0;

        foreach (ChunkPropsMarkupData markupData in worldSettings.PropMarkupDatas)
        {
            if (markupData == null)
            {
                removedMarkupDatas++;
                continue;
            }

            if (markupData.propMarkupDatas == null)
            {
                markupData.propMarkupDatas = new PropMarkupData[0];
                EditorUtility.SetDirty(markupData);
            }

            List<PropMarkupData> validProps = new List<PropMarkupData>();
            foreach (PropMarkupData prop in markupData.propMarkupDatas)
            {
                if (prop == null || prop.propDefinition == null)
                {
                    removedProps++;
                    continue;
                }

                // Check for duplicate or invalid GUIDs
                while (prop.guid == 0 || usedGuids.Contains(prop.guid))
                {
                    prop.guid = GenerateUniqueGuid(usedGuids);
                    reassignedGuids++;
                }
                usedGuids.Add(prop.guid);
                validProps.Add(prop);
            }

            markupData.propMarkupDatas = validProps.ToArray();
            if (validProps.Count > 0)
            {
                validMarkupDatas.Add(markupData);
                EditorUtility.SetDirty(markupData);
            }
            else
            {
                removedMarkupDatas++;
            }
        }

        // Update WorldSettings with valid markup datas
        worldSettings.PropMarkupDatas.Clear();
        worldSettings.PropMarkupDatas.AddRange(validMarkupDatas);
        EditorUtility.SetDirty(worldSettings);

        Debug.Log($"Clean Up World Settings: Removed {removedMarkupDatas} invalid MarkupDatas, {removedProps} invalid props, reassigned {reassignedGuids} GUIDs. Valid MarkupDatas: {validMarkupDatas.Count}, Total Props: {usedGuids.Count}.");
    }

    private int GenerateUniqueGuid(HashSet<int> usedGuids)
    {
        int newGuid = usedGuids.Count > 0 ? usedGuids.Max() + 1 : 1;
        while (usedGuids.Contains(newGuid))
        {
            newGuid++;
        }
        return newGuid;
    }

    private int GetUniqueGuid(WorldSettings worldSettings)
    {
        HashSet<int> usedGuids = new HashSet<int>();
        foreach (ChunkPropsMarkupData markupData in worldSettings.PropMarkupDatas)
        {
            if (markupData != null && markupData.propMarkupDatas != null)
            {
                foreach (PropMarkupData prop in markupData.propMarkupDatas)
                {
                    if (prop != null)
                    {
                        usedGuids.Add(prop.guid);
                    }
                }
            }
        }
        return GenerateUniqueGuid(usedGuids);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isAddingPoints || newPropDefinition == null)
            return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;
        LevelEditor manager = (LevelEditor)target;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Calculate chunk coordinate
                FChunkPosition chunkCoord = manager.WorldSettings.GetChunkCoordFromPosition(hit.point);
                ChunkPropsMarkupData markupData = manager.WorldSettings.GetOrCreateMarkupData(chunkCoord);

                Undo.RecordObject(markupData, "Add Prop Point");

                List<PropMarkupData> points = markupData.propMarkupDatas != null
                    ? new List<PropMarkupData>(markupData.propMarkupDatas)
                    : new List<PropMarkupData>();

                int guid = GetUniqueGuid(manager.WorldSettings);

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

        if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit previewHit))
        {
            Mesh mesh = manager.GetMeshFromPrefab(newPropDefinition.prefab);
            Vector3 forward = useSurfaceNormal ? previewHit.normal : forwardDirection;

            if (mesh != null && newPropDefinition.prefab != null)
            {
                Handles.color = new Color(1f, 1f, 0f, 0.5f);
                Quaternion rotation = forward.sqrMagnitude > 0 ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
                Vector3 scale = newPropDefinition.prefab.transform.localScale;
                Bounds bounds = mesh.bounds;
                Handles.DrawWireCube(previewHit.point + rotation * bounds.center, rotation * Vector3.Scale(bounds.size, scale));
            }
            else
            {
                Handles.color = Color.yellow;
                Handles.DrawSolidDisc(previewHit.point, previewHit.normal, 0.3f);
            }
        }
        sceneView.Repaint();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
}