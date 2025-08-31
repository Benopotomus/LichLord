#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(PropMarker))]
public class PropMarkerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Snap to Ground", GUILayout.Height(40)))
        {
            foreach (var t in targets)
            {
                var marker = t as PropMarker;
                if (marker != null)
                {
                    SnapToGround(marker);
                }
            }
        }
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
}
#endif
