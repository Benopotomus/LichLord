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
        private BuildableZoneReplicator _floorPrefab;

        [SerializeField]
        private BoxCollider _boxCollider;

        [Networked]
        private ref FWorldTransform _transform => ref MakeRef<FWorldTransform>();

        private List<BuildableZoneReplicator> _replicators = new List<BuildableZoneReplicator>(8);

        public void AddReplicator(BuildableZoneReplicator replicator)
        {
            if (!_replicators.Contains(replicator))
            {
                _replicators.Add(replicator);
            }
        }

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
            // make sure there is enough room for a floor
            var replicator = GetReplicatorWithFreeWallSlots(orientation);
            int freeIndex = replicator.GetFreeWallIndex(orientation);

            // then place the wall on the floor
            replicator.PlaceBuildableWall(freeIndex, definitionID, orientation, x, y, z);
        }

        public void PlaceBuildableFloor(int definitionID, int x, int y, int z)
        {
            // make sure there is enough room for this wall
            var replicator = GetReplicatorWithFreeFloorSlots();
            int freeIndex = replicator.GetFreeFloorIndex();

            // then place the wall on the floor
            replicator.PlaceBuildableFloor(freeIndex, definitionID, x, y, z);
        }

        public void PlaceBuildableFeature(int definitionID, EWallOrientation orientation, int x, int y, int z)
        {
            // make sure there is enough room for a floor
            var replicator = GetReplicatorWithFreeFeatureSlots();
            int freeIndex = replicator.GetFreeFeatureIndex();

            // then place the wall on the floor
            replicator.PlaceBuildableWall(freeIndex, definitionID, orientation, x, y, z);
        }

        private BuildableZoneReplicator SpawnReplicator()
        {
            var replicator = Runner.Spawn(
                _floorPrefab,
                transform.position,
                Quaternion.identity,
                inputAuthority: null,
                onBeforeSpawned: (runner, spawnedObject) =>
                {
                    // This is your spawn delegate
                    var rep = spawnedObject.GetComponent<BuildableZoneReplicator>();
                    rep.Initialized(this);
                });

            return replicator;
        }

        public BuildableZoneReplicator GetReplicatorWithFreeFloorSlots()
        {
            foreach (var replicator in _replicators)
            {
                if (replicator.FreeIndicesFloor.Count > 0)
                    return replicator;
            }

            return SpawnReplicator();
        }

        public BuildableZoneReplicator GetReplicatorWithFreeWallSlots(EWallOrientation orientation)
        {
            foreach (var replicator in _replicators)
            {
                switch (orientation)
                {
                    case EWallOrientation.North:
                        if (replicator.FreeIndicesWallNorth.Count > 0)
                            return replicator;
                        break;
                    case EWallOrientation.South:
                        if (replicator.FreeIndicesWallSouth.Count > 0)
                            return replicator;
                        break;
                    case EWallOrientation.West:
                        if (replicator.FreeIndicesWallWest.Count > 0)
                            return replicator;
                        break;
                    case EWallOrientation.East:
                        if (replicator.FreeIndicesWallEast.Count > 0)
                            return replicator;
                        break;
                }
            }

            return SpawnReplicator();
        }

        public BuildableZoneReplicator GetReplicatorWithFreeFeatureSlots()
        {
            foreach (var replicator in _replicators)
            {
                if (replicator.FreeIndicesFeature.Count > 0)
                    return replicator;
            }

            return SpawnReplicator();
        }
    }
}
