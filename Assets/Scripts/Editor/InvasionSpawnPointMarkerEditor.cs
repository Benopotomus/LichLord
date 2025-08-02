#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InvasionSpawnPointMarker))]
[CanEditMultipleObjects]
public class InvasionSpawnPointMarkerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Snap to Ground", GUILayout.Height(40)))
        {
            foreach (var t in targets)
            {
                InvasionSpawnPointMarker marker = t as InvasionSpawnPointMarker;
                if (marker != null)
                    SnapToGround(marker);
            }
        }
    }

    private void SnapToGround(InvasionSpawnPointMarker marker)
    {
        Vector3 origin = marker.transform.position + Vector3.up * 5f;
        Vector3 direction = Vector3.down;
        float maxDistance = 1000f;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance);

        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(marker.transform))
                continue;

            Undo.RecordObject(marker.transform, "Snap Marker To Ground");
            marker.transform.position = hit.point;
            EditorUtility.SetDirty(marker.transform);
            return;
        }

        Debug.LogWarning($"Snap to Ground: No ground found below marker {marker.name}.");
    }
}
#endif
