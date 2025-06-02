using UnityEngine;
using LichLord.Props;

public class LevelPropsMarkupManager : MonoBehaviour
{
    [SerializeField] private LevelPropsMarkupData levelPropsMarkupData;
    public LevelPropsMarkupData LevelPropsMarkup => levelPropsMarkupData;

    private void OnDrawGizmos()
    {
        if (levelPropsMarkupData == null || levelPropsMarkupData.propMarkupDatas == null) return;

        foreach (PropMarkupData point in levelPropsMarkupData.propMarkupDatas)
        {
            if (point.propDefinition == null) continue;

            Mesh mesh = GetMeshFromPrefab(point.propDefinition.prefab);
            Color color = point.propDefinition.propName switch
            {
                "Tree" => Color.green,
                "Rock" => Color.gray,
                _ => Color.blue
            };

            if (mesh != null)
            {
                Gizmos.color = new Color(color.r, color.g, color.b, 0.8f);
                Quaternion rotation = point.rotation;
                Gizmos.DrawMesh(mesh, point.position, rotation, point.propDefinition.prefab.transform.localScale);
            }
            else
            {
                Gizmos.color = color;
                Gizmos.DrawSphere(point.position, 0.5f);
            }
        }
    }

    public Mesh GetMeshFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
            return meshFilter.sharedMesh;

        SkinnedMeshRenderer skinnedMeshRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            return skinnedMeshRenderer.sharedMesh;

        return null;
    }
}