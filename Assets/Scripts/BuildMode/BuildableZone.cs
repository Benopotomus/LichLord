using Fusion;
using System.Collections.Generic;
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
        private BuildableZoneFloor _floorPrefab;

        [SerializeField]
        private BoxCollider _boxCollider;

        [Networked]
        private ref FWorldTransform _transform => ref MakeRef<FWorldTransform>();

        private List<BuildableZoneFloor> _floors = new List<BuildableZoneFloor>(8);

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

        public void PlaceBuildableWall(int definitionID, EWallOrientation orientation, int x, int y, int z)
        {
            // make sure there is a floor for this level
            if (y >= _floors.Count)
            {
                SpawnMissingFloors(y);
            }

            // then place the wall on the floor
            _floors[y].PlaceBuildableWall(definitionID, orientation, x, y, z);
        }

        public void PlaceBuildableFloor(int definitionID, int x, int y, int z)
        {
            // make sure there is a floor for this level
            if (y >= _floors.Count)
            {
                SpawnMissingFloors(y);
            }

            // then place the wall on the floor
            _floors[y].PlaceBuildableFloor(definitionID, x, y, z);
        }

        private void SpawnMissingFloors(int floorY)
        {
            // spawn missing floors up to y
            for (int i = _floors.Count; i <= floorY; i++)
            {
                var spawnPosition = transform.position + Vector3.up * i * Grid.TileSizeY; // assuming 2m floor height
                var floorInstance = Runner.Spawn(_floorPrefab, spawnPosition, Quaternion.identity, inputAuthority: null);
                _floors.Add(floorInstance);
                floorInstance.Initialized(this);
            }
        }
    }
}
