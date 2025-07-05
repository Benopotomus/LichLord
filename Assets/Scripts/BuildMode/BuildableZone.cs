using Fusion;
using UnityEngine;

namespace LichLord.Buildables
{
    [RequireComponent(typeof(BoxCollider))]
    public class BuildableZone : ContextBehaviour
    {
        [SerializeField]
        private BuildableGrid _grid;
        public BuildableGrid Grid => _grid;

        [SerializeField]
        private BuildableSpawner _spawner;

        [SerializeField]
        private BoxCollider _boxCollider;

        [Networked]
        private ref FWorldTransform _transform => ref MakeRef<FWorldTransform>();

        [Networked, Capacity(256)]
        protected virtual NetworkArray<FBuildFloorData> _tileFloors { get; }

        [Networked, Capacity(256)]
        protected virtual NetworkArray<FBuildWallData> _tileWallsNorth { get; }

        [Networked, Capacity(256)]
        protected virtual NetworkArray<FBuildWallData> _tileWallsEast { get; }

        [Networked, Capacity(256)]
        protected virtual NetworkArray<FBuildWallData> _tileWallsSouth { get; }

        [Networked, Capacity(256)]
        protected virtual NetworkArray<FBuildWallData> _tileWallsWest { get; }

        // store last-rendered data
        private FBuildFloorData[] _previousFloors = new FBuildFloorData[256];
        private FBuildWallData[] _previousWallsNorth = new FBuildWallData[256];
        private FBuildWallData[] _previousWallsSouth = new FBuildWallData[256];
        private FBuildWallData[] _previousWallsEast = new FBuildWallData[256];
        private FBuildWallData[] _previousWallsWest = new FBuildWallData[256];

        private void Reset()
        {
            // Ensure BoxCollider is set up correctly in editor when component is added or reset
            SetupCollider();
        }

        private void OnValidate()
        {
            // Called when values change in inspector, update collider size
            SetupCollider();
        }

        private void Awake()
        {
            SetupCollider();
        }

        private void SetupCollider()
        {
            if (_grid == null)
                return;

            _boxCollider = GetComponent<BoxCollider>();

            // Calculate size
            float sizeX = _grid.GridSizeX * _grid.TileSizeXZ;
            float sizeY = _grid.GridSizeY * _grid.TileSizeXZ;
            float sizeZ = _grid.GridSizeZ * _grid.TileSizeXZ;

            // Set BoxCollider size and center
            _boxCollider.size = new Vector3(sizeX, sizeY, sizeZ);  // height of 2 is arbitrary, adjust if needed
            _boxCollider.center = new Vector3(sizeX / 2f, sizeY / 2f, sizeZ / 2f);  // center so box covers grid area

            _boxCollider.isTrigger = true;
        }

        public void PlaceBuildableFloor(int definitionID, int x, int y, int z)
        { 
        
        }

        public void PlaceBuildableWall(int definitionID, EWallOrientation orientation, int x, int y, int z)
        {
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
            {
                Debug.LogError($"Index out of bounds: x={x} z={z}");
                return;
            }

            switch (orientation)
            {
                case EWallOrientation.North:
                    ref FBuildWallData wallDataNorth = ref _tileWallsNorth.GetRef<FBuildWallData>(x + (z * 16));
                    wallDataNorth.WallDefinitionID = (byte)definitionID;
                    break;
                case EWallOrientation.South:
                    ref FBuildWallData wallDataSouth = ref _tileWallsNorth.GetRef<FBuildWallData>(x + (z * 16));
                    wallDataSouth.WallDefinitionID = (byte)definitionID;
                    break;
                case EWallOrientation.East:
                    ref FBuildWallData wallDataEast = ref _tileWallsNorth.GetRef<FBuildWallData>(x + (z * 16));
                    wallDataEast.WallDefinitionID = (byte)definitionID;
                    break;
                case EWallOrientation.West:
                    ref FBuildWallData wallDataWest = ref _tileWallsNorth.GetRef<FBuildWallData>(x + (z * 16));
                    wallDataWest.WallDefinitionID = (byte)definitionID;
                    break;
            }
        }

        public override void Render()
        {
            for (int i = 0; i < 256; i++)
            {
                // check Floor
                var currentFloor = _tileFloors.GetRef(i);
                if (currentFloor.FloorDefinitionID != _previousFloors[i].FloorDefinitionID)
                {
                    UpdateFloorVisual(i, currentFloor);
                    _previousFloors[i] = currentFloor;
                }

                // check North wall
                var currentNorth = _tileWallsNorth.GetRef(i);
                if (currentNorth.WallDefinitionID != _previousWallsNorth[i].WallDefinitionID)
                {
                    UpdateWallVisual(i, EWallOrientation.North, currentNorth);
                    _previousWallsNorth[i] = currentNorth;
                }

                // check South wall
                var currentSouth = _tileWallsSouth.GetRef(i);
                if (currentSouth.WallDefinitionID != _previousWallsSouth[i].WallDefinitionID)
                {
                    UpdateWallVisual(i, EWallOrientation.South, currentSouth);
                    _previousWallsSouth[i] = currentSouth;
                }

                // check East wall
                var currentEast = _tileWallsEast.GetRef(i);
                if (currentEast.WallDefinitionID != _previousWallsEast[i].WallDefinitionID)
                {
                    UpdateWallVisual(i, EWallOrientation.East, currentEast);
                    _previousWallsEast[i] = currentEast;
                }

                // check West wall
                var currentWest = _tileWallsWest.GetRef(i);
                if (currentWest.WallDefinitionID != _previousWallsWest[i].WallDefinitionID)
                {
                    UpdateWallVisual(i, EWallOrientation.West, currentWest);
                    _previousWallsWest[i] = currentWest;
                }
            }
        }

        private void UpdateFloorVisual(int index, FBuildFloorData floorData)
        {
            // you can calculate x/z/y from index if needed:
            int x = index % 16;
            int z = index / 16;

            Debug.Log($"[Render] Floor changed at ({x},{z}) -> {floorData.FloorDefinitionID}");

            // TODO: update floor mesh or prefab instance here
        }

        private void UpdateWallVisual(int index, EWallOrientation orientation, FBuildWallData wallData)
        {
            int x = index % 16;
            int z = index / 16;

            Debug.Log($"[Render] Wall {orientation} changed at ({x},{z}) -> {wallData.WallDefinitionID}");

            // TODO: update wall mesh or prefab instance here

            if (wallData.WallDefinitionID == 0)
                return;

            BuildableDefinition definition = Global.Tables.BuildableTable.TryGetDefinition(wallData.WallDefinitionID);
            
            Vector3 worldPosition = transform.position + new Vector3(Grid.TileSizeXZ * x, 0 , Grid.TileSizeXZ * z);

            Quaternion rotation = transform.rotation * GetRotationForWall(orientation);
            Debug.Log("Start Rotation " + rotation);

            _spawner.SpawnBuildable(this, definition, worldPosition, rotation, 0);
        }

        private Quaternion GetRotationForWall(EWallOrientation orientation)
        {
            switch (orientation)
            {
                case EWallOrientation.North:
                    return Quaternion.identity; // 0 degrees
                case EWallOrientation.East:
                    return Quaternion.Euler(0, 90, 0);
                case EWallOrientation.South:
                    return Quaternion.Euler(0, 180, 0);
                case EWallOrientation.West:
                    return Quaternion.Euler(0, 270, 0);
                default:
                    return Quaternion.identity;
            }
        }
    }
}
