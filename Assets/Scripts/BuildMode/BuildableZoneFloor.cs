using Fusion;
using LichLord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableZoneFloor : ContextBehaviour
    {
        private BuildableZone _zone;

        [SerializeField]
        private BuildableSpawner _spawner;

        [SerializeField]
        private BuildableWallSpawner _wallSpawner;

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

        private Dictionary<int, Buildable> _buildableWallsNorth = new Dictionary<int, Buildable>();
        private Dictionary<int, Buildable> _buildableWallsSouth = new Dictionary<int, Buildable>();
        private Dictionary<int, Buildable> _buildableWallsEast = new Dictionary<int, Buildable>();
        private Dictionary<int, Buildable> _buildableWallsWest = new Dictionary<int, Buildable>();

        public void Initialized(BuildableZone zone)
        {
            _zone = zone;
            _wallSpawner.OnBuildableWallSpawned += OnBuildableWallSpawned;
        }

        public void PlaceBuildableFloor(int definitionID, int x, int y, int z)
        {
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
            {
                Debug.LogError($"Index out of bounds: x={x} z={z}");
                return;
            }
            
            int index = x + (z * 16);

            _tileFloors.GetRef(index).FloorDefinitionID = (byte)definitionID;
        }

        public void PlaceBuildableWall(int definitionID, EWallOrientation orientation, int x, int y, int z)
        {
            if (x < 0 || x >= 16 || z < 0 || z >= 16)
            {
                Debug.LogError($"Index out of bounds: x={x} z={z}");
                return;
            }

            int index = x + (z * 16);

            switch (orientation)
            {
                case EWallOrientation.North:
                    _tileWallsNorth.GetRef(index).WallDefinitionID = (byte)definitionID;
                    break;
                case EWallOrientation.South:
                    _tileWallsSouth.GetRef(index).WallDefinitionID = (byte)definitionID;
                    break;
                case EWallOrientation.East:
                    _tileWallsEast.GetRef(index).WallDefinitionID = (byte)definitionID;
                    break;
                case EWallOrientation.West:
                    _tileWallsWest.GetRef(index).WallDefinitionID = (byte)definitionID;
                    break;
            }
        }

        int _lastRenderFrame = -1;
        public override void Render()
        {
            if (Runner.Tick == _lastRenderFrame)
                return;

            _lastRenderFrame = Runner.Tick;

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
            {
                // remove the mesh
                DespawnBuildableWall(index, orientation);
                return;
            }

            BuildableDefinition definition = Global.Tables.BuildableWallTable.TryGetDefinition(wallData.WallDefinitionID);

            BuildableUtility.GetWallWorldTransform(_zone, x, 0, z, orientation, out Vector3 worldPosition, out Quaternion rotation);

            worldPosition.y = transform.position.y;

            _wallSpawner.SpawnBuildableWall(this, index, orientation, definition, worldPosition, rotation, 0);
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
    }
}
