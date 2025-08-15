using Fusion;
using UnityEngine;

namespace LichLord.Buildables
{
    [ExecuteAlways]
    public class BuildableGrid : MonoBehaviour
    {
        public int GridSizeX = 16;
        public int GridSizeY = 4;
        public int GridSizeZ = 16;
        public float TileSizeXZ = 5f;
        public float TileSizeY = 3.5f;

        public int SubGridSizeX => GridSizeX * 8;
        public int SubGridSizeZ => GridSizeZ * 8;

        public Mesh FloorMesh;
        public Material FloorMaterial;

        [SerializeField]
        private GameObject _floorMeshObject;

        // Remove Start and OnValidate calls

        // Create floor mesh object if needed
        private void CreateFloorMeshObject()
        {
            if (_floorMeshObject == null)
            {
                _floorMeshObject = new GameObject("FloorMesh");
                _floorMeshObject.transform.SetParent(transform, false);
                _floorMeshObject.AddComponent<MeshFilter>();
                _floorMeshObject.AddComponent<MeshRenderer>();
            }
        }

        // This is now the public method you call manually
        public void RegenerateFloorMesh()
        {
            CreateFloorMeshObject();

            if (FloorMesh == null || FloorMaterial == null || _floorMeshObject == null)
                return;

            var meshFilter = _floorMeshObject.GetComponent<MeshFilter>();
            var meshRenderer = _floorMeshObject.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = FloorMaterial;

            Mesh combinedMesh = new Mesh();
            CombineInstance[] combine = new CombineInstance[GridSizeX * GridSizeZ];

            Vector3 tileScale = new Vector3(TileSizeXZ * 0.95f, 0.05f, TileSizeXZ * 0.95f);

            int i = 0;
            for (int x = 0; x < GridSizeX; x++)
            {
                for (int z = 0; z < GridSizeZ; z++)
                {
                    var pos = new Vector3(
                        x * TileSizeXZ + TileSizeXZ * 0.5f,
                        0,
                        z * TileSizeXZ + TileSizeXZ * 0.5f
                    );
                    Matrix4x4 transformMatrix = Matrix4x4.TRS(pos, Quaternion.identity, tileScale);

                    combine[i].mesh = FloorMesh;
                    combine[i].transform = transformMatrix;
                    i++;
                }
            }

            combinedMesh.CombineMeshes(combine, true, true);
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

        public bool TryGetSubGridPosition(Vector3 worldPos, out int subTileX, out int tileY, out int subTileZ)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            // Main tile index
            int tileX = Mathf.FloorToInt(localPos.x / TileSizeXZ);
            tileY = GetNearestFloorLevel(localPos.y);
            int tileZ = Mathf.FloorToInt(localPos.z / TileSizeXZ);

            // Bounds check
            bool insideX = tileX >= 0 && tileX < GridSizeX;
            bool insideY = tileY >= 0 && tileY < GridSizeY;
            bool insideZ = tileZ >= 0 && tileZ < GridSizeZ;

            if (!insideX || !insideY || !insideZ)
            {
                subTileX = subTileZ = 0;
                return false;
            }

            // Position within current tile
            float localXInTile = localPos.x - tileX * TileSizeXZ;
            float localZInTile = localPos.z - tileZ * TileSizeXZ;

            // Sub-tile size
            float subTileSize = TileSizeXZ / 8f;

            // Global sub-tile indices
            subTileX = tileX * 8 + Mathf.FloorToInt(localXInTile / subTileSize);
            subTileZ = tileZ * 8 + Mathf.FloorToInt(localZInTile / subTileSize);

            // Clamp to prevent overflow due to floating-point precision
            subTileX = Mathf.Clamp(subTileX, 0, GridSizeX * 8 - 1);
            subTileZ = Mathf.Clamp(subTileZ, 0, GridSizeZ * 8 - 1);

            return true;
        }

        public bool TryGetWallPosition(Vector3 worldPos, out int gridX, out int gridY, out int gridZ, out EWallOrientation orientation)
        {
            orientation = default;

            if (!TryGetGridPosition(worldPos, out gridX, out gridY, out gridZ))
                return false;

            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            Vector3 tileOrigin = new Vector3(gridX * TileSizeXZ, gridY * TileSizeY, gridZ * TileSizeXZ);

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
                return false;

            int countClosest = 0;
            if (Mathf.Abs(distWest - minDist) < epsilon) { orientation = EWallOrientation.West; countClosest++; }
            if (Mathf.Abs(distEast - minDist) < epsilon) { orientation = EWallOrientation.East; countClosest++; }
            if (Mathf.Abs(distSouth - minDist) < epsilon) { orientation = EWallOrientation.South; countClosest++; }
            if (Mathf.Abs(distNorth - minDist) < epsilon) { orientation = EWallOrientation.North; countClosest++; }

            return countClosest == 1;
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
