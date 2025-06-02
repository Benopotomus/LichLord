using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using LichLord.Props;
using System.IO;

[CustomEditor(typeof(LevelPropsMarkupManager))]
public class LevelPropsMarkupManagerEditor : Editor
{
    private PropDefinition newPropDefinition;
    private bool isAddingPoints = false;
    private Vector3 forwardDirection = Vector3.forward;
    private bool useSurfaceNormal = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelPropsMarkupManager manager = (LevelPropsMarkupManager)target;

        if (manager.LevelPropsMarkup == null)
        {
            EditorGUILayout.HelpBox("Please assign a LevelPropsMarkupData to the Prop Data field.", MessageType.Warning);
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
            Undo.RecordObject(manager.LevelPropsMarkup, "Clear Prop Points");
            manager.LevelPropsMarkup.propMarkupDatas = new PropMarkupData[0];
            EditorUtility.SetDirty(manager.LevelPropsMarkup);
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
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isAddingPoints || newPropDefinition == null)
            return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;
        LevelPropsMarkupManager manager = (LevelPropsMarkupManager)target;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Undo.RecordObject(manager.LevelPropsMarkup, "Add Prop Point");

                List<PropMarkupData> points = manager.LevelPropsMarkup.propMarkupDatas != null
                    ? new List<PropMarkupData>(manager.LevelPropsMarkup.propMarkupDatas)
                    : new List<PropMarkupData>();

                int guid = points.Count > 0 ? points[points.Count - 1].guid + 1 : 0;

                points.Add(new PropMarkupData
                {
                    guid = guid,
                    position = hit.point,
                    rotation = Quaternion.LookRotation((useSurfaceNormal ? hit.normal : forwardDirection).normalized, Vector3.up),
                    propDefinition = newPropDefinition
                });

                manager.LevelPropsMarkup.propMarkupDatas = points.ToArray();
                EditorUtility.SetDirty(manager.LevelPropsMarkup);

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