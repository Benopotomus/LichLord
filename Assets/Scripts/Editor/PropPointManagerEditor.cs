using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using LichLord.Props;

[CustomEditor(typeof(PropPointManager))]
public class PropPointManagerEditor : Editor
{
    private PropDefinition newPropDefinition;
    private bool isAddingPoints = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PropPointManager manager = (PropPointManager)target;

        if (manager.PropData == null)
        {
            EditorGUILayout.HelpBox("Please assign a PropPointDataAsset to the Prop Data field.", MessageType.Warning);
            return;
        }

        // Input field for PropDefinition
        newPropDefinition = EditorGUILayout.ObjectField("Prop Definition", newPropDefinition, typeof(PropDefinition), false) as PropDefinition;

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
            Undo.RecordObject(manager.PropData, "Clear Prop Points");
            manager.PropData.propPoints = new PropPointData[0];
            EditorUtility.SetDirty(manager.PropData);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isAddingPoints || newPropDefinition == null) return;

        // Disable default selection behavior
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;
        PropPointManager manager = (PropPointManager)target;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Undo.RecordObject(manager.PropData, "Add Prop Point");

                List<PropPointData> points = manager.PropData.propPoints != null
                    ? new List<PropPointData>(manager.PropData.propPoints)
                    : new List<PropPointData>();

                points.Add(new PropPointData
                {
                    position = hit.point,
                    propDefinition = newPropDefinition
                });

                manager.PropData.propPoints = points.ToArray();
                EditorUtility.SetDirty(manager.PropData);

                e.Use();
            }
        }

        // Draw preview
        if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit previewHit))
        {
            Mesh mesh = GetMeshFromPrefab(newPropDefinition.prefab);
            if (mesh != null && newPropDefinition.prefab != null)
            {
                // Draw mesh preview with transparency
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Yellow, semi-transparent
                Gizmos.DrawMesh(mesh, previewHit.point, Quaternion.identity, newPropDefinition.prefab.transform.localScale);
            }
            else
            {
                // Fallback to disc
                Handles.color = Color.yellow;
                Handles.DrawSolidDisc(previewHit.point, previewHit.normal, 0.3f);
            }
        }

        sceneView.Repaint();
    }

    private Mesh GetMeshFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh;
        }

        SkinnedMeshRenderer skinnedMeshRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
        {
            return skinnedMeshRenderer.sharedMesh;
        }

        return null;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
}