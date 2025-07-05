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
        public int TileSizeXZ = 5;
        public float TileSizeY = 3.5f;



        public Mesh FloorMesh;
        public Material FloorMaterial;

        private Vector3 _tileScale = new Vector3(0.99f, 1, 0.99f);
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

            int i = 0;
            for (int x = 0; x < GridSizeX; x++)
            {
                for (int y = 0; y < GridSizeZ; y++)
                {
                    var pos = new Vector3(
                        x * TileSizeXZ + TileSizeXZ * 0.5f - 2.5f,
                        0,
                        y * TileSizeXZ + TileSizeXZ * 0.5f + 2.5f
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
            // Convert world position to local position relative to grid origin (this.transform.position)
            Vector3 localPos = worldPos - transform.position;

            // Calculate grid indices by dividing local position by TileSize
            gridX = Mathf.FloorToInt(localPos.x / TileSizeXZ);
            gridY = Mathf.CeilToInt(localPos.y / TileSizeY);
            gridZ = Mathf.FloorToInt(localPos.z / TileSizeXZ);

            // Check if inside grid bounds
            bool insideX = gridX >= 0 && gridX < GridSizeX;
            bool insideY = gridY >= 0 && gridY < GridSizeY;
            bool insideZ = gridZ >= 0 && gridZ < GridSizeZ;

            return insideX && insideY && insideZ;
        }

        /// <summary>
        /// Given a world position, tries to find which wall line it is closest to, and returns the wall orientation and grid position.
        /// If the point is within a "dead zone" (center 3 units of the tile), returns false.
        /// </summary>
        public bool TryGetWallPosition(Vector3 worldPos, out int gridX, out int gridY, out int gridZ, out EWallOrientation orientation)
        {
            // convert world to local
            Vector3 localPos = worldPos - transform.position;

            // work out which tile we are in
            int tileX = Mathf.FloorToInt(localPos.x / TileSizeXZ);
            int tileY = Mathf.FloorToInt((localPos.y + 0.001f) / TileSizeY) ;
            int tileZ = Mathf.FloorToInt(localPos.z / TileSizeXZ);

            // get relative offset within the tile [0..TileSize]
            float localXInTile = localPos.x - tileX * TileSizeXZ;
            float localZInTile = localPos.z - tileZ * TileSizeXZ;

            // define deadzone around center of the tile (±1.5 from 2.5)
            float centerMin = (TileSizeXZ * 0.5f) - 1.5f;
            float centerMax = (TileSizeXZ * 0.5f) + 1.5f;

            if (localXInTile >= centerMin && localXInTile <= centerMax &&
                localZInTile >= centerMin && localZInTile <= centerMax)
            {
                // deadzone, no wall
                gridX = gridY = gridZ = 0;
                orientation = default;
                return false;
            }

            // now figure out which wall line is closest
            // measure distance to left/right walls
            float distToWest = localXInTile;
            float distToEast = TileSizeXZ - localXInTile;

            // measure distance to south/north walls
            float distToSouth = localZInTile;
            float distToNorth = TileSizeXZ - localZInTile;

            // pick closest of the four
            float minDist = Mathf.Min(distToWest, distToEast, distToSouth, distToNorth);

            if (minDist == distToWest)
            {
                orientation = EWallOrientation.West;
                gridX = tileX;
                gridZ = tileZ;
            }
            else if (minDist == distToEast)
            {
                orientation = EWallOrientation.East;
                gridX = tileX + 1;
                gridZ = tileZ;
            }
            else if (minDist == distToSouth)
            {
                orientation = EWallOrientation.South;
                gridX = tileX;
                gridZ = tileZ;
            }
            else // distToNorth
            {
                orientation = EWallOrientation.North;
                gridX = tileX;
                gridZ = tileZ + 1;
            }

            gridY = tileY;

            // validate
            bool insideX = gridX >= 0 && gridX <= GridSizeX;
            bool insideY = gridY >= 0 && gridY < GridSizeY;
            bool insideZ = gridZ >= 0 && gridZ <= GridSizeZ;

            if (insideX && insideY && insideZ)
                return true;

            return false;
        }

    }

    public enum EWallOrientation : byte
    {
        None,
        North,
        South,
        East,
        West
    }
}
