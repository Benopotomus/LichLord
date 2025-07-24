using UnityEditor;
using UnityEngine;
using LichLord.Buildables;

[CustomEditor(typeof(BuildableGrid))]
public class BuildableGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BuildableGrid grid = (BuildableGrid)target;
        if (GUILayout.Button("Regenerate Floor Mesh"))
        {
            grid.RegenerateFloorMesh();
        }
    }
}