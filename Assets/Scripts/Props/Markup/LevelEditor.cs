using UnityEngine;
using LichLord.Props;

namespace LichLord.World
{
    public class LevelEditor : MonoBehaviour
    {
        [SerializeField] private WorldSettings worldSettings;
        public WorldSettings WorldSettings => worldSettings;

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
}
