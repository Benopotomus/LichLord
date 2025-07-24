using Fusion;
using LichLord.Props;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableZoneReplicator : ContextBehaviour, IStateAuthorityChanged
    {
        [Networked]
        private BuildableZone _zone { get; set; }

        [SerializeField]
        private BuildableFloorSpawner _floorSpawner;

        [SerializeField]
        private BuildableWallSpawner _wallSpawner;

        [SerializeField]
        private BuildableFeatureSpawner _featureSpawner;

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

        [Networked, Capacity(256)]
        protected virtual NetworkArray<FBuildFeatureData> _tileFeatures { get; }

        // store last-rendered data
        private FBuildFloorData[] _previousFloors = new FBuildFloorData[256];
        private FBuildWallData[] _previousWallsNorth = new FBuildWallData[256];
        private FBuildWallData[] _previousWallsSouth = new FBuildWallData[256];
        private FBuildWallData[] _previousWallsEast = new FBuildWallData[256];
        private FBuildWallData[] _previousWallsWest = new FBuildWallData[256];
        private FBuildFeatureData[] _previousFeatures = new FBuildFeatureData[256];

        private Dictionary<int, Buildable> _buildableFloors = new Dictionary<int, Buildable>();
        private Dictionary<int, Buildable> _buildableWallsNorth = new Dictionary<int, Buildable>();
        private Dictionary<int, Buildable> _buildableWallsSouth = new Dictionary<int, Buildable>();
        private Dictionary<int, Buildable> _buildableWallsEast = new Dictionary<int, Buildable>();
        private Dictionary<int, Buildable> _buildableWallsWest = new Dictionary<int, Buildable>();
        private Dictionary<int, Buildable> _buildableFeatures = new Dictionary<int, Buildable>();

        private HashSet<int> _freeIndicesFloor = new HashSet<int>();
        public IReadOnlyCollection<int> FreeIndicesFloor => _freeIndicesFloor;

        private HashSet<int> _freeIndicesWallNorth = new HashSet<int>();
        public IReadOnlyCollection<int> FreeIndicesWallNorth => _freeIndicesWallNorth;

        private HashSet<int> _freeIndicesWallSouth = new HashSet<int>();
        public IReadOnlyCollection<int> FreeIndicesWallSouth => _freeIndicesWallSouth;

        private HashSet<int> _freeIndicesWallEast = new HashSet<int>();
        public IReadOnlyCollection<int> FreeIndicesWallEast => _freeIndicesWallEast;

        private HashSet<int> _freeIndicesWallWest = new HashSet<int>();
        public IReadOnlyCollection<int> FreeIndicesWallWest => _freeIndicesWallWest;

        private HashSet<int> _freeIndicesFeature = new HashSet<int>();
        public IReadOnlyCollection<int> FreeIndicesFeature => _freeIndicesFeature;

        public void Initialized(BuildableZone zone)
        {
            _zone = zone;
        }

        public void StateAuthorityChanged()
        {
            Debug.Log($"StateAuthority Changed, HasStateAuthority: {HasStateAuthority}");
            if (!HasStateAuthority)
                return;

            RebuildFreeIndices();
        }

        public int GetFreeFloorIndex()
        {
            if (_freeIndicesFloor.Count > 0)
            {
                return _freeIndicesFloor.First();
            }
            return -1;
        }

        public int GetFreeWallIndex(EWallOrientation orientation)
        {
            switch(orientation)
            { 
                case EWallOrientation.North:
                    if (_freeIndicesWallNorth.Count > 0)
                        return _freeIndicesWallNorth.First();
                    break;
                case EWallOrientation.South:
                    if (_freeIndicesWallSouth.Count > 0)
                        return _freeIndicesWallSouth.First();
                    break;
                case EWallOrientation.East:
                    if (_freeIndicesWallEast.Count > 0)
                        return _freeIndicesWallEast.First();
                    break;
                case EWallOrientation.West:
                    if (_freeIndicesWallWest.Count > 0)
                        return _freeIndicesWallWest.First();
                    break;
            }

            return -1;
        }

        public int GetFreeFeatureIndex()
        {
            if (_freeIndicesFeature.Count > 0)
            {
                return _freeIndicesFeature.First();
            }
            return -1;
        }

        public override void Spawned()
        {
            base.Spawned();
            _floorSpawner.OnBuildableFloorSpawned += OnBuildableFloorSpawned;
            _wallSpawner.OnBuildableWallSpawned += OnBuildableWallSpawned;
            _featureSpawner.OnBuildableFeatureSpawned += OnBuildableFeatureSpawned;
            _zone.AddReplicator(this);

            for (int i = 0; i < BuildableConstants.MAX_BUILDABLE_REPS; i++)
            {
                _freeIndicesFloor.Add(i); // Initially, all indices are free
                _freeIndicesWallNorth.Add(i);
                _freeIndicesWallSouth.Add(i);
                _freeIndicesWallEast.Add(i);
                _freeIndicesWallWest.Add(i);
                _freeIndicesFeature.Add(i);
            }
        }

        private void RebuildFreeIndices()
        {
            _freeIndicesFloor.Clear();
            for (int i = 0; i < BuildableConstants.MAX_BUILDABLE_REPS; i++)
            {
                if (_tileFloors.GetRef(i).FloorDefinitionID == 0)
                    _freeIndicesFloor.Add(i);

                if (_tileWallsNorth.GetRef(i).WallDefinitionID == 0)
                    _freeIndicesWallNorth.Add(i);

                if (_tileWallsSouth.GetRef(i).WallDefinitionID == 0)
                    _freeIndicesWallSouth.Add(i);

                if (_tileWallsEast.GetRef(i).WallDefinitionID == 0)
                    _freeIndicesWallEast.Add(i);

                if (_tileWallsWest.GetRef(i).WallDefinitionID == 0)
                    _freeIndicesWallWest.Add(i);
            }
        }

        public void PlaceBuildableFloor(int index, int definitionID, int x, int y, int z)
        {
            if (x < 0 || x >= _zone.Grid.GridSizeX ||
                y < 0 || y >= _zone.Grid.GridSizeY ||
                z < 0 || z >= _zone.Grid.GridSizeZ)
            {
                Debug.LogError($"Index out of bounds: x={x} z={z}");
                return;
            }

            ref FBuildFloorData floorData = ref _tileFloors.GetRef(index);

            bool wasActive = floorData.FloorDefinitionID > 0;
            bool willBeActive = definitionID > 0;

            floorData.FloorDefinitionID = (byte)definitionID;
            floorData.GridX = (byte)x;
            floorData.GridY = (byte)y;
            floorData.GridZ = (byte)z;

            if (!willBeActive && wasActive)
                _freeIndicesFloor.Add(index); // NPC became inactive
            else if (willBeActive && !wasActive)
                _freeIndicesFloor.Remove(index); // NPC became active
        }

        public void PlaceBuildableWall(int index, int definitionID, EWallOrientation orientation, int x, int y, int z)
        {
            if (x < 0 || x >= _zone.Grid.GridSizeX ||
                y < 0 || y >= _zone.Grid.GridSizeY ||
                z < 0 || z >= _zone.Grid.GridSizeZ)
            {
                Debug.LogError($"Index out of bounds: x={x} z={z}");
                return;
            }

            var walls = new[]
            {
                _tileWallsNorth,
                _tileWallsSouth,
                _tileWallsEast,
                _tileWallsWest
            };

            var freeIndices = new[]
            {
                _freeIndicesWallNorth,
                _freeIndicesWallSouth,
                _freeIndicesWallEast,
                _freeIndicesWallWest
            };

            int dir = orientation switch
            {
                EWallOrientation.North => 0,
                EWallOrientation.South => 1,
                EWallOrientation.East => 2,
                EWallOrientation.West => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation))
            };

            ref FBuildWallData wallData = ref walls[dir].GetRef(index);

            bool wasActive = wallData.WallDefinitionID > 0;
            bool willBeActive = definitionID > 0;

            wallData.WallDefinitionID = (byte)definitionID;
            wallData.GridX = (byte)x;
            wallData.GridY = (byte)y;
            wallData.GridZ = (byte)z;

            if (!willBeActive && wasActive)
                freeIndices[dir].Add(index);
            else if (willBeActive && !wasActive)
                freeIndices[dir].Remove(index);
        }

        public void PlaceBuildableFeature(int index, int definitionID, int subGridX, int y, int subGridZ)
        {
            if (subGridX < 0 || subGridX >= _zone.Grid.SubGridSizeX ||
                y < 0 || y >= _zone.Grid.GridSizeY ||
                subGridZ < 0 || subGridZ >= _zone.Grid.SubGridSizeZ)
            {
                Debug.LogError($"Index out of bounds: x={subGridX} z={subGridZ}");
                return;
            }

            ref FBuildFeatureData featureData = ref _tileFeatures.GetRef(index);

            bool wasActive = featureData.FeatureDefinitionID > 0;
            bool willBeActive = definitionID > 0;

            featureData.FeatureDefinitionID = (byte)definitionID;
            featureData.SubGridX = (byte)subGridX;
            featureData.GridY = (byte)y;
            featureData.SubGridZ = (byte)subGridZ;

            if (!willBeActive && wasActive)
                _freeIndicesFeature.Add(index);
            else if (willBeActive && !wasActive)
                _freeIndicesFeature.Remove(index);
        }

        int _lastRenderFrame = -1;
        public override void Render()
        {
            if (Runner.Tick == _lastRenderFrame)
                return;

            _lastRenderFrame = Runner.Tick;

            for (int i = 0; i < BuildableConstants.MAX_BUILDABLE_REPS; i++)
            {
                var currentFloor = _tileFloors.GetRef(i);
                if (currentFloor.FloorDefinitionID != _previousFloors[i].FloorDefinitionID)
                {
                    UpdateFloorVisual(i, currentFloor);
                    _previousFloors[i] = currentFloor;
                }

                var currentNorth = _tileWallsNorth.GetRef(i);
                if (currentNorth.WallDefinitionID != _previousWallsNorth[i].WallDefinitionID)
                {
                    UpdateWallVisual(i, EWallOrientation.North, currentNorth);
                    _previousWallsNorth[i] = currentNorth;
                }

                var currentSouth = _tileWallsSouth.GetRef(i);
                if (currentSouth.WallDefinitionID != _previousWallsSouth[i].WallDefinitionID)
                {
                    UpdateWallVisual(i, EWallOrientation.South, currentSouth);
                    _previousWallsSouth[i] = currentSouth;
                }

                var currentEast = _tileWallsEast.GetRef(i);
                if (currentEast.WallDefinitionID != _previousWallsEast[i].WallDefinitionID)
                {
                    UpdateWallVisual(i, EWallOrientation.East, currentEast);
                    _previousWallsEast[i] = currentEast;
                }

                var currentWest = _tileWallsWest.GetRef(i);
                if (currentWest.WallDefinitionID != _previousWallsWest[i].WallDefinitionID)
                {
                    UpdateWallVisual(i, EWallOrientation.West, currentWest);
                    _previousWallsWest[i] = currentWest;
                }

                var currentFeature = _tileFeatures.GetRef(i);
                if (currentFeature.FeatureDefinitionID != _previousFeatures[i].FeatureDefinitionID)
                {
                    UpdateFeatureVisual(i, currentFeature);
                    _previousFeatures[i] = currentFeature;
                }
            }
        }

        private void UpdateFloorVisual(int index, FBuildFloorData floorData)
        {
            BuildableDefinition definition = Global.Tables.BuildableFloorTable.TryGetDefinition(floorData.FloorDefinitionID);

            BuildableUtility.GetFloorWorldTransform(
                _zone,
                floorData.GridX,
                floorData.GridY,
                floorData.GridZ,
                out Vector3 worldPosition,
                out Quaternion rotation);

            // for example, you could do:
            _floorSpawner.SpawnBuildableFloor(this, index, definition, worldPosition, rotation, 0);
        }

        private void UpdateWallVisual(int index, EWallOrientation orientation, FBuildWallData wallData)
        {
            if (wallData.WallDefinitionID == 0)
            {
                // remove the mesh
                DespawnBuildableWall(index, orientation);
                return;
            }

            BuildableDefinition definition = Global.Tables.BuildableWallTable.TryGetDefinition(wallData.WallDefinitionID);

            BuildableUtility.GetWallWorldTransform(_zone, 
                wallData.GridX, 
                wallData.GridY, 
                wallData.GridZ, 
                orientation, 
                out Vector3 worldPosition, 
                out Quaternion rotation);

            _wallSpawner.SpawnBuildableWall(this, index, orientation, definition, worldPosition, rotation, 0);
        }

        private void UpdateFeatureVisual(int index, FBuildFeatureData featureData)
        {
            if (featureData.FeatureDefinitionID == 0)
            {
                DespawnBuildableFeature(index);
                return;
            }

            BuildableDefinition definition = Global.Tables.BuildableFeatureTable.TryGetDefinition(featureData.FeatureDefinitionID);

            BuildableUtility.GetFloorWorldTransform(
                _zone,
                featureData.SubGridX,
                featureData.GridY,
                featureData.SubGridZ,
                out Vector3 worldPosition,
                out Quaternion rotation
            );

            _featureSpawner.SpawnBuildableFeature(this, index, definition, worldPosition, rotation, 0);
        }

        public bool WallExistsAtOrAdjacent(EWallOrientation orientation, int x, int y, int z)
        {
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
                return false;

            int index = x + (z * 16);

            // Check current tile's wall
            byte currentWallID = 0;
            switch (orientation)
            {
                case EWallOrientation.North:
                    currentWallID = _tileWallsNorth.GetRef(index).WallDefinitionID;
                    break;
                case EWallOrientation.South:
                    currentWallID = _tileWallsSouth.GetRef(index).WallDefinitionID;
                    break;
                case EWallOrientation.East:
                    currentWallID = _tileWallsEast.GetRef(index).WallDefinitionID;
                    break;
                case EWallOrientation.West:
                    currentWallID = _tileWallsWest.GetRef(index).WallDefinitionID;
                    break;
            }
            if (currentWallID != 0)
                return true;

            // Check adjacent tile's opposite wall
            int adjX = x;
            int adjZ = z;
            EWallOrientation oppositeOrientation;

            switch (orientation)
            {
                case EWallOrientation.North:
                    adjZ = z + 1;
                    oppositeOrientation = EWallOrientation.South;
                    break;
                case EWallOrientation.South:
                    adjZ = z - 1;
                    oppositeOrientation = EWallOrientation.North;
                    break;
                case EWallOrientation.East:
                    adjX = x + 1;
                    oppositeOrientation = EWallOrientation.West;
                    break;
                case EWallOrientation.West:
                    adjX = x - 1;
                    oppositeOrientation = EWallOrientation.East;
                    break;
                default:
                    return false;
            }

            if (adjX < 0 || adjX >= 16 || adjZ < 0 || adjZ >= 16)
                return false;

            int adjIndex = adjX + (adjZ * 16);
            byte adjWallID = 0;

            switch (oppositeOrientation)
            {
                case EWallOrientation.North:
                    adjWallID = _tileWallsNorth.GetRef(adjIndex).WallDefinitionID;
                    break;
                case EWallOrientation.South:
                    adjWallID = _tileWallsSouth.GetRef(adjIndex).WallDefinitionID;
                    break;
                case EWallOrientation.East:
                    adjWallID = _tileWallsEast.GetRef(adjIndex).WallDefinitionID;
                    break;
                case EWallOrientation.West:
                    adjWallID = _tileWallsWest.GetRef(adjIndex).WallDefinitionID;
                    break;
            }

            return adjWallID != 0;
        }

        private void OnBuildableFloorSpawned(Buildable buildable, int index)
        {
            _buildableFloors[index] = buildable;
        }

        private void OnBuildableWallSpawned(Buildable buildable, int index, EWallOrientation orientation)
        {
            switch (orientation)
            {
                case EWallOrientation.North:
                    _buildableWallsNorth[index] = buildable;
                    break;
                case EWallOrientation.South:
                    _buildableWallsSouth[index] = buildable;
                    break;
                case EWallOrientation.East:
                    _buildableWallsEast[index] = buildable;
                    break;
                case EWallOrientation.West:
                    _buildableWallsWest[index] = buildable;
                    break;
            }
        }

        private void OnBuildableFeatureSpawned(Buildable buildable, int index)
        {
            _buildableFeatures[index] = buildable;
        }

        private void DespawnBuildableWall(int index, EWallOrientation orientation)
        {
            switch (orientation)
            {
                case EWallOrientation.North:
                    if(_buildableWallsNorth.TryGetValue(index, out Buildable buildableNorth))
                        buildableNorth.StartRecycle();

                    _buildableWallsNorth.Remove(index);
                    break;
                case EWallOrientation.South:
                    if (_buildableWallsSouth.TryGetValue(index, out Buildable buildableSouth))
                        buildableSouth.StartRecycle();

                    _buildableWallsSouth.Remove(index);
                    break;
                case EWallOrientation.East:
                    if (_buildableWallsEast.TryGetValue(index, out Buildable buildableEast))
                        buildableEast.StartRecycle();

                    _buildableWallsEast.Remove(index);
                    break;
                case EWallOrientation.West:
                    if (_buildableWallsWest.TryGetValue(index, out Buildable buildableWest))
                        buildableWest.StartRecycle();

                    _buildableWallsWest.Remove(index);
                    break;
            }
        }

        private void DespawnBuildableFloor(int index)
        {
            if (_buildableFloors.TryGetValue(index, out Buildable buildable))
            {
                buildable.StartRecycle();
                _buildableFloors.Remove(index);
            }
        }

        private void DespawnBuildableFeature(int index)
        {
            if (_buildableFeatures.TryGetValue(index, out Buildable buildable))
            {
                buildable.StartRecycle();
                _buildableFeatures.Remove(index);
            }
        }
    }
}
