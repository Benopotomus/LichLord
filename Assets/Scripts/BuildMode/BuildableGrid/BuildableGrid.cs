using Fusion;
using UnityEngine;

namespace LichLord.Buildables
{
    [ExecuteAlways]
    public class BuildableGrid : ContextBehaviour
    {
        public int GridSizeX = 16;
        public int GridSizeY = 4;
        public int GridSizeZ = 16;
        public float TileSizeXZ = 5f;
        public float TileSizeY = 3.5f;

        public Mesh FloorMesh;
        public Material FloorMaterial;
        private GameObject _floorMeshObject;

        private void Start()
        {
            BuildFloorMesh();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) // in case the object was deleted
                    BuildFloorMesh();
            };
#endif
        }

        private void BuildFloorMesh()
        {
            if (FloorMesh == null || FloorMaterial == null)
                return;

            if (_floorMeshObject == null)
            {
                _floorMeshObject = GameObject.Find("FloorMesh");
                if (_floorMeshObject == null)
                {
                    _floorMeshObject = new GameObject("FloorMesh");
                    _floorMeshObject.transform.SetParent(transform, false);
                    _floorMeshObject.AddComponent<MeshFilter>();
                    _floorMeshObject.AddComponent<MeshRenderer>();
                }
            }

            var meshFilter = _floorMeshObject.GetComponent<MeshFilter>();
            var meshRenderer = _floorMeshObject.GetComponent<MeshRenderer>();
            meshRenderer.material = FloorMaterial;

            // generate the mesh
            Mesh combinedMesh = new Mesh();
            CombineInstance[] combine = new CombineInstance[GridSizeX * GridSizeZ];

            Vector3 _tileScale = new Vector3(TileSizeXZ * 0.95f, 0.25f, TileSizeXZ * 0.95f);

            int i = 0;
            for (int x = 0; x < GridSizeX; x++)
            {
                for (int y = 0; y < GridSizeZ; y++)
                {
                    var pos = new Vector3(
                        x * TileSizeXZ + TileSizeXZ * 0.5f,
                        0,
                        y * TileSizeXZ + TileSizeXZ * 0.5f
                    );
                    Matrix4x4 transformMatrix = Matrix4x4.TRS(pos, Quaternion.identity, _tileScale);

                    combine[i].mesh = FloorMesh;
                    combine[i].transform = transformMatrix;
                    i++;
                }
            }

            combinedMesh.CombineMeshes(combine, true, true);

            // assign to meshfilter
            meshFilter.sharedMesh = combinedMesh;
        }

        public bool TryGetGridPosition(Vector3 worldPos, out int gridX, out int gridY, out int gridZ)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            gridX = Mathf.FloorToInt(localPos.x / TileSizeXZ);
            gridY = GetNearestFloorLevel(localPos.y);
            gridZ = Mathf.FloorToInt(localPos.z / TileSizeXZ);

            bool insideX = gridX >= 0 && gridX < GridSizeX;
            bool insideY = gridY >= 0 && gridY < GridSizeY;
            bool insideZ = gridZ >= 0 && gridZ < GridSizeZ;

            return insideX && insideY && insideZ;
        }

        public bool TryGetWallPosition(Vector3 worldPos, out int gridX, out int gridY, out int gridZ, out EWallOrientation orientation)
        {
            orientation = default;

            // Use TryGetGridPosition to get x/y/z
            if (!TryGetGridPosition(worldPos, out gridX, out gridY, out gridZ))
            {
                return false;
            }

            // We know it’s in bounds now
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            // reconstruct the tile’s local origin
            Vector3 tileOrigin = new Vector3(gridX * TileSizeXZ, gridY * TileSizeY, gridZ * TileSizeXZ);

            // calculate wall centers
            Vector3 westCenter = tileOrigin + new Vector3(0f, 0f, TileSizeXZ * 0.5f);
            Vector3 eastCenter = tileOrigin + new Vector3(TileSizeXZ, 0f, TileSizeXZ * 0.5f);
            Vector3 southCenter = tileOrigin + new Vector3(TileSizeXZ * 0.5f, 0f, 0f);
            Vector3 northCenter = tileOrigin + new Vector3(TileSizeXZ * 0.5f, 0f, TileSizeXZ);

            float distWest = Vector3.Distance(localPos, westCenter);
            float distEast = Vector3.Distance(localPos, eastCenter);
            float distSouth = Vector3.Distance(localPos, southCenter);
            float distNorth = Vector3.Distance(localPos, northCenter);

            float minDist = Mathf.Min(distWest, distEast, distSouth, distNorth);
            float epsilon = 0.001f;
            float maxDistance = TileSizeXZ * 0.75f;

            if (minDist > maxDistance)
            {
                return false;
            }

            int countClosest = 0;
            if (Mathf.Abs(distWest - minDist) < epsilon) { orientation = EWallOrientation.West; countClosest++; }
            if (Mathf.Abs(distEast - minDist) < epsilon) { orientation = EWallOrientation.East; countClosest++; }
            if (Mathf.Abs(distSouth - minDist) < epsilon) { orientation = EWallOrientation.South; countClosest++; }
            if (Mathf.Abs(distNorth - minDist) < epsilon) { orientation = EWallOrientation.North; countClosest++; }

            if (countClosest != 1)
            {
                return false; // ambiguous
            }

            return true;
        }

        private int GetNearestFloorLevel(float localY)
        {
            int baseY = Mathf.FloorToInt(localY / TileSizeY);
            if (baseY < 0) baseY = 0;

            float lowerCenterY = baseY * TileSizeY;
            float upperCenterY = (baseY + 1) * TileSizeY;

            float distToLower = Mathf.Abs(localY - lowerCenterY);
            float distToUpper = Mathf.Abs(localY - upperCenterY);

            return (distToUpper < distToLower) ? baseY + 1 : baseY;
        }
    }

   
}
