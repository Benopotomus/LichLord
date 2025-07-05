using Fusion;
using System;
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
        private Material _previewMaterial;

        private GameObject _ghostPreview;
        private MeshFilter _previewFilter;
        private MeshRenderer _previewRenderer;

        [Header("Buildable Inventory")]
        [SerializeField] private List<BuildableDefinition> _availableBuildables = new List<BuildableDefinition>();
        public IReadOnlyList<BuildableDefinition> AvailableBuildables => _availableBuildables;


        [SerializeField]
        private BuildableDefinition _selectedDefinition;

        [Networked] private sbyte _selectedIndex { get; set; }

        [Header("Placement")]
        [SerializeField] private BuildableZone _targetZone;

        [SerializeField] bool _placementValid;
        [SerializeField] private Vector3Int _gridPosition;
        [SerializeField] private EWallOrientation _orientation;

        private void Start()
        {
            // Setup ghost preview mesh
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
            UpdateActionSelection(0);
        }

        public void ProcessInput(ref FGameplayInput input)
        {
            ProcessBuildableSelection(ref input);

            ProcessSelectedGridArea();

            ProcessBuildableActions(ref input);
        }

        public void OnFixedUpdate()
        {

        }

        private void ProcessBuildableSelection(ref FGameplayInput input)
        {
            int newIndex = -1;
            if (input.ScrollDelta != 0 && _availableBuildables.Count > 1)
            {
                int delta = input.ScrollDelta > 0 ? 1 : -1;
                newIndex = (_selectedIndex + delta + _availableBuildables.Count) % _availableBuildables.Count;
                //Debug.Log($"[ActionManager] ScrollDelta={input.ScrollDelta}, Delta={delta}, NewIndex={newIndex}");
            }

            if (input.ActionSelection > 0)
            {
                Debug.Log($"[BuilderComponent] ActionSelection={input.ActionSelection}");
                newIndex = input.ActionSelection - 1;
            }

            if (newIndex >= _availableBuildables.Count)
            {
                return;
            }

            if (newIndex < 0)
                return;

            if (newIndex == _selectedIndex)
                return;

            UpdateActionSelection(newIndex);
        }

        private void UpdateActionSelection(int newIndex)
        {
            if (!HasStateAuthority)
                return;

            if (_selectedIndex >= 0 && _selectedIndex < _availableBuildables.Count)
            {
                //_availableBuildables[_selectedIndex].DeselectAction(_pc, Runner);
            }

            _selectedIndex = (sbyte)newIndex;
            _selectedDefinition = _availableBuildables[newIndex];
        }

        private void ProcessBuildableActions(ref FGameplayInput input)
        {
            if (!input.Fire)
                return;

            switch (_selectedDefinition.PlacementType)
            { 
                case EBuildablePlacementType.Wall:
                    Context.BuildableManager.RPC_PlaceBuildableWall(_targetZone,
                        _orientation,
                        (byte)_gridPosition.x,
                        (byte)_gridPosition.y,
                        (byte)_gridPosition.z,
                        (byte)_selectedDefinition.TableID);
                    break;
                case EBuildablePlacementType.Floor:
                    Context.BuildableManager.RPC_PlaceBuildableFloor(_targetZone,
                        (byte)_gridPosition.x,
                        (byte)_gridPosition.y,
                        (byte)_gridPosition.z,
                        (byte)_selectedDefinition.TableID);
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
                _gridPosition = new Vector3Int(0, 0, 0);
                _orientation = EWallOrientation.None;
            }

            SetGhostVisibility(hasValidSelection);
        }

        public void SetGhostVisibility(bool newVisiblity)
        {
            _ghostPreview.SetActive(newVisiblity);
        }

        private bool UpdateGridPosition(Vector3 position)
        {
            if (_targetZone.Grid.TryGetGridPosition(position, out int gridX, out int gridY, out int gridZ))
            {
                _gridPosition = new Vector3Int(gridX, gridY, gridZ);

                var grid = _targetZone.Grid;

                Vector3 cellCenter = new Vector3(
                    gridX * grid.TileSizeXZ + grid.TileSizeXZ * 0.5f,
                    gridY * grid.TileSizeY,
                    gridZ * grid.TileSizeXZ + grid.TileSizeXZ * 0.5f
                ) + grid.transform.position;

                _ghostPreview.transform.position = cellCenter;
                _ghostPreview.transform.rotation = Quaternion.identity;
                _ghostPreview.transform.localScale = new Vector3(
                    grid.TileSizeXZ,
                    0.25f, // thin Y preview
                    grid.TileSizeXZ
                );

                //Debug.Log($"Grid: {gridX}, {gridY}, {gridZ}");
                return true;
            }

            return false;
        }

        private bool UpdateWallPosition(Vector3 position)
        {
            if (_targetZone.Grid.TryGetWallPosition(position, out int gridX, out int gridY, out int gridZ, out EWallOrientation orientation))
            {
                // Set the placement data
                _gridPosition = new Vector3Int(gridX, gridY, gridZ);
                _orientation = orientation;

                var grid = _targetZone.Grid;
                float halfSizeXZ = grid.TileSizeXZ * 0.5f;
                float halfSizeY = grid.TileSizeY * 0.5f;

                Vector3 wallCenter = Vector3.zero;

                switch (orientation)
                {
                    case EWallOrientation.North:
                        wallCenter = new Vector3(
                            gridX * grid.TileSizeXZ + halfSizeXZ,
                            gridY * grid.TileSizeY + halfSizeY,
                            gridZ * grid.TileSizeXZ
                        );
                        _ghostPreview.transform.localScale = new Vector3(
                            grid.TileSizeXZ, grid.TileSizeY, 0.2f
                        );
                        break;

                    case EWallOrientation.South:
                        wallCenter = new Vector3(
                            gridX * grid.TileSizeXZ + halfSizeXZ,
                            gridY * grid.TileSizeY + halfSizeY,
                            gridZ * grid.TileSizeXZ
                        );
                        _ghostPreview.transform.localScale = new Vector3(
                            grid.TileSizeXZ, grid.TileSizeY, 0.2f
                        );
                        break;

                    case EWallOrientation.East:
                        wallCenter = new Vector3(
                            gridX * grid.TileSizeXZ,
                            gridY * grid.TileSizeY + halfSizeY,
                            gridZ * grid.TileSizeXZ + halfSizeXZ
                        );
                        _ghostPreview.transform.localScale = new Vector3(
                            0.2f, grid.TileSizeY, grid.TileSizeXZ
                        );
                        break;

                    case EWallOrientation.West:
                        wallCenter = new Vector3(
                            gridX * grid.TileSizeXZ,
                            gridY * grid.TileSizeY + halfSizeY,
                            gridZ * grid.TileSizeXZ + halfSizeXZ
                        );
                        _ghostPreview.transform.localScale = new Vector3(
                            0.2f, grid.TileSizeY, grid.TileSizeXZ
                        );
                        break;
                }

                _ghostPreview.transform.position = wallCenter + grid.transform.position;
                _ghostPreview.transform.rotation = Quaternion.identity;

                //Debug.Log($"Wall: {gridX}, {gridY}, {gridZ}, {orientation}");
                return true;
            }

            return false;
        }

        public BuildableDefinition GetSelectedBuildable()
        {
            if (_selectedIndex < 0)
                return null;

            return _availableBuildables[_selectedIndex];
        }
    }
}
