using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuilderComponent : ContextBehaviour
    {
        [SerializeField]
        private PlayerCharacter _pc;

        [Header("Preview Settings")]
        [SerializeField]
        private Mesh _previewMesh;

        [SerializeField]
        private Mesh _previewMeshArrow;

        [SerializeField]
        private Material _previewMaterial;

        private GameObject _ghostPreview;
        private MeshFilter _previewFilter;
        private MeshRenderer _previewRenderer;

        private EBuildableCategory _buildableCateogory = EBuildableCategory.Wall;
        public EBuildableCategory BuildableCategory => _buildableCateogory;

        [Header("Buildable Inventory")]
        [SerializeField] private List<BuildableWallDefinition> _availableBuildableWalls = new List<BuildableWallDefinition>();
        public IReadOnlyList<BuildableWallDefinition> AvailableBuildableWalls => _availableBuildableWalls;

        [SerializeField] private List<BuildableFloorDefinition> _availableBuildableFloors = new List<BuildableFloorDefinition>();
        public IReadOnlyList<BuildableFloorDefinition> AvailableBuildableFloors => _availableBuildableFloors;

        [SerializeField]
        private BuildableDefinition _selectedDefinition;

        [Networked] private sbyte _selectedIndex { get; set; }

        [Header("Placement")]
        [SerializeField] private BuildableZone _targetZone;

        [SerializeField] bool _placementValid;
        [SerializeField] private Vector3Int _gridPosition;
        [SerializeField] private EWallOrientation _orientation;

        private bool _isDeleteMode = false;
        public bool IsDeleteMode => _isDeleteMode;

        private void Start()
        {
            _ghostPreview = new GameObject("GhostPreview");
            _ghostPreview.transform.SetParent(transform, false);

            _previewFilter = _ghostPreview.AddComponent<MeshFilter>();
            _previewRenderer = _ghostPreview.AddComponent<MeshRenderer>();

            _previewFilter.mesh = _previewMesh;
            _previewRenderer.material = _previewMaterial;

            SetGhostVisibility(false);
        }

        public override void Spawned()
        {
            base.Spawned();
            UpdateCategorySelection(EBuildableCategory.Wall);
            UpdateActionSelection(0);
        }

        public void ProcessInput(ref FGameplayInput input)
        {
            ProcessBuildCategorySelection(ref input);
            ProcessBuildableSelection(ref input);

            ProcessSelectedGridArea();

            ProcessBuildableActions(ref input);
        }

        public void OnFixedUpdate()
        {
            // nothing yet
        }

        private void ProcessBuildCategorySelection(ref FGameplayInput input)
        {
            if (input.BuildCategory != EBuildableCategory.None && input.BuildCategory != _buildableCateogory)
            {
                UpdateCategorySelection(input.BuildCategory);
            }
        }

        private void UpdateCategorySelection(EBuildableCategory newCategory)
        {
            if (!HasStateAuthority)
                return;

            _buildableCateogory = newCategory;

            // reset to first available in the new category
            UpdateActionSelection(0);
        }

        private void ProcessBuildableSelection(ref FGameplayInput input)
        {
            int newIndex = -1;
            int listCount = 0;

            if (input.DeleteMode)
            {
                _isDeleteMode = !_isDeleteMode;
                return;  // short-circuit so no other selection runs
            }

            switch (_buildableCateogory)
            {
                case EBuildableCategory.Wall:
                    listCount = _availableBuildableWalls.Count;
                    break;
                case EBuildableCategory.Floor:
                    listCount = _availableBuildableFloors.Count;
                    break;
                    // you can add Roof/Interior here later
            }

            if (input.ScrollDelta != 0 && listCount > 1)
            {
                _isDeleteMode = false;
                int delta = input.ScrollDelta > 0 ? 1 : -1;
                newIndex = (_selectedIndex + delta + listCount) % listCount;
            }

            if (input.ActionSelection > 0)
            {
                _isDeleteMode = false;
                newIndex = input.ActionSelection - 1;
            }

            if (newIndex >= listCount || newIndex < 0)
                return;

            if (newIndex == _selectedIndex)
                return;

            UpdateActionSelection(newIndex);
        }

        private void UpdateActionSelection(int newIndex)
        {
            if (!HasStateAuthority)
                return;

            _selectedIndex = (sbyte)newIndex;

            switch (_buildableCateogory)
            {
                case EBuildableCategory.Wall:
                    _selectedDefinition = _availableBuildableWalls[newIndex];
                    break;

                case EBuildableCategory.Floor:
                    _selectedDefinition = _availableBuildableFloors[newIndex];
                    break;
            }
        }

        private void ProcessBuildableActions(ref FGameplayInput input)
        {
            if (!input.Fire)
                return;

            if (_isDeleteMode)
            {
                switch (_buildableCateogory)
                {
                    case EBuildableCategory.Wall:
                        Context.BuildableManager.RPC_PlaceBuildableWall(
                            _targetZone,
                            _orientation,
                            (byte)_gridPosition.x,
                            (byte)_gridPosition.y,
                            (byte)_gridPosition.z,
                            0
                        );
                        break;

                    case EBuildableCategory.Floor:
                        Context.BuildableManager.RPC_PlaceBuildableFloor(
                            _targetZone,
                            (byte)_gridPosition.x,
                            (byte)_gridPosition.y,
                            (byte)_gridPosition.z,
                            0
                        );
                        break;

                        // expand to roof/interior later
                }
                return;
            }

            switch (_selectedDefinition.PlacementType)
            {
                case EBuildablePlacementType.Wall:
                    Context.BuildableManager.RPC_PlaceBuildableWall(
                        _targetZone,
                        _orientation,
                        (byte)_gridPosition.x,
                        (byte)_gridPosition.y,
                        (byte)_gridPosition.z,
                        (byte)_selectedDefinition.TableID
                    );
                    break;
                case EBuildablePlacementType.Floor:
                    Context.BuildableManager.RPC_PlaceBuildableFloor(
                        _targetZone,
                        (byte)_gridPosition.x,
                        (byte)_gridPosition.y,
                        (byte)_gridPosition.z,
                        (byte)_selectedDefinition.TableID
                    );
                    break;
            }
        }

        private void ProcessSelectedGridArea()
        {
            FCachedRaycast cachedRaycast = Context.Camera.CachedRaycastHit;
            _targetZone = cachedRaycast.buildableZone;

            if (_targetZone == null || _selectedDefinition == null)
            {
                SetGhostVisibility(false);
                return;
            }

            bool hasValidSelection = false;

            switch (_selectedDefinition.PlacementType)
            {
                case EBuildablePlacementType.Floor:
                    hasValidSelection = UpdateGridPosition(cachedRaycast.position);
                    break;
                case EBuildablePlacementType.Wall:
                    hasValidSelection = UpdateWallPosition(cachedRaycast.position);
                    break;
            }

            _placementValid = hasValidSelection;

            if (!_placementValid)
            {
                _gridPosition = Vector3Int.zero;
                _orientation = EWallOrientation.None;
            }

            SetGhostVisibility(hasValidSelection);
        }

        public void SetGhostVisibility(bool newVisibility)
        {
            _ghostPreview.SetActive(newVisibility);
        }

        private bool UpdateGridPosition(Vector3 position)
        {
            if (_targetZone.Grid.TryGetGridPosition(position, out int gridX, out int gridY, out int gridZ))
            {
                _gridPosition = new Vector3Int(gridX, gridY, gridZ);

                var grid = _targetZone.Grid;

                Vector3 localCellCenter = new Vector3(
                    gridX * grid.TileSizeXZ + grid.TileSizeXZ * 0.5f,
                    gridY * grid.TileSizeY,
                    gridZ * grid.TileSizeXZ + grid.TileSizeXZ * 0.5f
                );

                Vector3 worldCellCenter = _targetZone.transform.TransformPoint(localCellCenter);

                _ghostPreview.transform.position = worldCellCenter;
                _ghostPreview.transform.rotation = _targetZone.transform.rotation;
                _ghostPreview.transform.localScale = new Vector3(
                    grid.TileSizeXZ,
                    0.25f,
                    grid.TileSizeXZ
                );

                return true;
            }
            return false;
        }

        private bool UpdateWallPosition(Vector3 position)
        {
            if (_targetZone.Grid.TryGetWallPosition(position, out int gridX, out int gridY, out int gridZ, out EWallOrientation orientation))
            {
                _gridPosition = new Vector3Int(gridX, gridY, gridZ);
                _orientation = orientation;

                BuildableUtility.GetWallWorldTransform(
                    _targetZone,
                    gridX,
                    gridY,
                    gridZ,
                    orientation,
                    out Vector3 worldPosition,
                    out Quaternion rotation
                );

                var grid = _targetZone.Grid;

                worldPosition.y += grid.TileSizeY * 0.5f;

                _ghostPreview.transform.position = worldPosition;
                _ghostPreview.transform.rotation = rotation;
                _ghostPreview.transform.localScale = new Vector3(
                    grid.TileSizeXZ,
                    grid.TileSizeY,
                    0.2f
                );

                return true;
            }
            return false;
        }

        public BuildableDefinition GetSelectedBuildable()
        {
            if (_selectedIndex < 0)
                return null;

            switch (_buildableCateogory)
            {
                case EBuildableCategory.Wall:
                    return _availableBuildableWalls[_selectedIndex];
                case EBuildableCategory.Floor:
                    return _availableBuildableFloors[_selectedIndex];
                // expand for Roof/Interior if you add them
                default:
                    return null;
            }
        }

        public IReadOnlyList<BuildableDefinition> ActiveBuildables
        {
            get
            {
                switch (_buildableCateogory)
                {
                    case EBuildableCategory.Wall:
                        return _availableBuildableWalls;
                    case EBuildableCategory.Floor:
                        return _availableBuildableFloors;
                    case EBuildableCategory.Roof:
                    case EBuildableCategory.Interior:
                        // expand these later if needed
                        return null;
                    default:
                        return null;
                }
            }
        }
    }

    public enum EBuildableCategory
    {
        None,
        Wall,
        Floor,
        Roof,
        Interior,
    }
}
