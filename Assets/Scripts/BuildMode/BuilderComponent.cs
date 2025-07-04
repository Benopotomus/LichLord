using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuilderComponent : ContextBehaviour
    {
        [SerializeField]
        private BuildableZone _targetZone;

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

        [SerializeField]
        private BuildableDefinition _selectedDefinition;

        [Header("Buildable Inventory")]
        [SerializeField] private List<BuildableDefinition> _availableBuildables = new List<BuildableDefinition>();
        public IReadOnlyList<BuildableDefinition> AvailableBuildables => _availableBuildables;

        [Networked] private sbyte _selectedIndex { get; set; }

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

        public void ProcessInput(ref FGameplayInput input)
        {
            ProcessBuildableSelection(ref input);
            ProcessBuildableActions(ref input);
            ProcessSelectedGridArea();
        }

        private void ProcessBuildableActions(ref FGameplayInput input)
        {
            if (input.Fire)
            { 
                // Attempt to build here

            }
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
                //Debug.Log($"[ActionManager] ActionSelection={input.ActionSelection}");
                newIndex = input.ActionSelection - 1;
            }

            if (newIndex >= _availableBuildables.Count)
            {
                //Debug.Log($"[ActionManager] Ignored invalid ActionSelection={input.ActionSelection} (exceeds availableActions.Count={availableActions.Count})");
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
            if (HasStateAuthority)
            {
                if (_selectedIndex >= 0 && _selectedIndex < _availableBuildables.Count)
                {
                    //_availableBuildables[_selectedIndex].DeselectAction(_pc, Runner);
                }

                _selectedIndex = (sbyte)newIndex;
                if (newIndex >= 0 && newIndex < _availableBuildables.Count)
                {
                   // _availableBuildables[newIndex].SelectAction(_pc, Runner);
                }

                if (newIndex >= 0 && newIndex < _availableBuildables.Count)
                {
                    Debug.Log($"[ActionManager] Selected action: {_availableBuildables[newIndex].BuildableName} (Index: {newIndex})");
                }
                else
                {
                    Debug.Log("[ActionManager] Action selection cleared");
                }
            }
        }

        private void ProcessSelectedGridArea()
        {
            FCachedRaycast cachedRaycast = Context.Camera.CachedRaycastHit;
            _targetZone = cachedRaycast.buildableZone;

            if (_targetZone == null)
            {
                _ghostPreview.SetActive(false);
                return;
            }

            // If no grid, try wall
            if (UpdateWallPosition(cachedRaycast.position))
                return;

            // Try grid first
            if (UpdateGridPosition(cachedRaycast.position))
                return;

            // No valid placement
            SetGhostVisibility(false);
        }

        public void SetGhostVisibility(bool newVisiblity)
        {
            _ghostPreview.SetActive(newVisiblity);
        }

        private bool UpdateGridPosition(Vector3 position)
        {
            if (_targetZone.Grid.TryGetGridPosition(position, out int gridX, out int gridY, out int gridZ))
            {
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

                SetGhostVisibility(true);

                //Debug.Log($"Grid: {gridX}, {gridY}, {gridZ}");
                return true;
            }

            return false;
        }

        private bool UpdateWallPosition(Vector3 position)
        {
            if (_targetZone.Grid.TryGetWallPosition(position, out int gridX, out int gridY, out int gridZ, out EWallOrientation orientation))
            {
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
                SetGhostVisibility(true);

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
