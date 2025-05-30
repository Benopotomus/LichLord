using UnityEngine;

namespace LichLord.Props
{
    public class PropPointManager : MonoBehaviour
    {
        [SerializeField] private PropPointMarkupData propData;
        public PropPointMarkupData PropData => propData; // Getter for editor access

        private void OnDrawGizmos()
        {
            if (propData == null || propData.propPoints == null) return;

            foreach (PropPointData point in propData.propPoints)
            {
                if (point.propDefinition == null) continue;

                // Get mesh from prefab
                Mesh mesh = GetMeshFromPrefab(point.propDefinition.prefab);
                Color color = point.propDefinition.propName switch
                {
                    "Tree" => Color.green,
                    "Rock" => Color.gray,
                    _ => Color.blue
                };

                if (mesh != null)
                {
                    // Draw mesh with slight transparency
                    Gizmos.color = new Color(color.r, color.g, color.b, 0.8f);
                    Gizmos.DrawMesh(mesh, point.position, Quaternion.identity, point.propDefinition.prefab.transform.localScale);
                }
                else
                {
                    // Fallback to sphere if no mesh
                    Gizmos.color = color;
                    Gizmos.DrawSphere(point.position, 0.5f);
                }
            }
        }

        private Mesh GetMeshFromPrefab(GameObject prefab)
        {
            if (prefab == null) return null;

            // Try MeshFilter (for static meshes)
            MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                return meshFilter.sharedMesh;
            }

            // Try SkinnedMeshRenderer (for animated models)
            SkinnedMeshRenderer skinnedMeshRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            return null;
        }
    }
}