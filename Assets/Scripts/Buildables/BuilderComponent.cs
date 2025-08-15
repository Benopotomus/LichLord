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

        [SerializeField]
        private EBuildableCategory _buildableCategory = EBuildableCategory.Wall;
        public EBuildableCategory BuildableCategory => _buildableCategory;

        [Header("Buildable Inventory")]
        [SerializeField] private List<BuildableWallDefinition> _availableBuildableWalls = new List<BuildableWallDefinition>();
        public IReadOnlyList<BuildableWallDefinition> AvailableBuildableWalls => _availableBuildableWalls;

        [SerializeField] private List<BuildableFloorDefinition> _availableBuildableFloors = new List<BuildableFloorDefinition>();
        public IReadOnlyList<BuildableFloorDefinition> AvailableBuildableFloors => _availableBuildableFloors;

        [SerializeField] private List<BuildableFeatureDefinition> _availableBuildableFeatures = new List<BuildableFeatureDefinition>();
        public IReadOnlyList<BuildableFeatureDefinition> AvailableBuildableFeatures => _availableBuildableFeatures;

        [SerializeField]
        private BuildableDefinition _selectedDefinition;

        [Networked] private sbyte _selectedIndex { get; set; }

        [Header("Placement")]
        [SerializeField] private BuildableZone _targetZone;

        [SerializeField] bool _placementValid;

        private bool _isDeleteMode = false;
        public bool IsDeleteMode => _isDeleteMode;

        [SerializeField] Vector3 _placementPosition;
        [SerializeField] Quaternion _placementRotation;

        private BuildablePreviewLoader _loader = new BuildablePreviewLoader();

        private void OnBuildableSpawned(GameObject go)
        {
            if (_ghostPreview != null)
            {
                Destroy(_ghostPreview);
            }

            _ghostPreview = Instantiate(go);

            foreach (var collider in _ghostPreview.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            _loader.OnBuildablePreviewLoaded += OnBuildableSpawned;
            SetGhostVisibility(false);
            UpdateCategorySelection(EBuildableCategory.Wall);
        }

        public void ProcessInput(ref FGameplayInput input)
        {
            ProcessBuildCategorySelection(ref input);
            ProcessBuildableSelection(ref input);
            ProcessPlacement(ref input);
            ProcessBuildableActions(ref input);
        }

        private void ProcessBuildCategorySelection(ref FGameplayInput input)
        {
            if (input.BuildCategory != EBuildableCategory.None && input.BuildCategory != _buildableCategory)
            {
                UpdateCategorySelection(input.BuildCategory);
            }
        }

        private void UpdateCategorySelection(EBuildableCategory newCategory)
        {
            if (!HasStateAuthority)
                return;

            _buildableCategory = newCategory;

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

            switch (_buildableCategory)
            {
                case EBuildableCategory.Wall:
                    listCount = _availableBuildableWalls.Count;
                    break;
                case EBuildableCategory.Floor:
                    listCount = _availableBuildableFloors.Count;
                    break;
                case EBuildableCategory.Feature:
                    listCount = _availableBuildableFeatures.Count;
                    break;
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
            _selectedIndex = (sbyte)newIndex;

            switch (_buildableCategory)
            {
                case EBuildableCategory.Wall:
                    UpdateSelectedDefinition(_availableBuildableWalls[newIndex]);
                    break;
                case EBuildableCategory.Floor:
                    UpdateSelectedDefinition(_availableBuildableFloors[newIndex]);
                    break;
                case EBuildableCategory.Feature:
                    UpdateSelectedDefinition(_availableBuildableFeatures[newIndex]);
                    break;
            }
        }

        private void UpdateSelectedDefinition(BuildableDefinition newDefinition)
        {
            if (_selectedDefinition == newDefinition)
                return;

            _selectedDefinition = newDefinition;
            
            _loader.SpawnBuildablePreview(_selectedDefinition);
        }

        private void ProcessBuildableActions(ref FGameplayInput input)
        {
            if (!input.Fire)
                return;

            if (!_placementValid)
                return;

            FWorldTransform spawnTransform = new FWorldTransform();
            spawnTransform.Position = _placementPosition;
            spawnTransform.Rotation = _placementRotation;

            _targetZone.RPC_PlaceBuildable((ushort)_selectedDefinition.TableID, spawnTransform);

            if (_isDeleteMode)
            {

            }
        }

        private void ProcessPlacement(ref FGameplayInput input)
        {
            if (input.RotateBuildableYaw)
            {
                Vector3 euler = _placementRotation.eulerAngles;
                euler.y += 22.5f; // Tick yaw up
                _placementRotation = Quaternion.Euler(euler);
            }

            FCachedRaycast cachedRaycast = Context.Camera.CachedRaycastHit;
            _targetZone = cachedRaycast.buildableZone;

            if (_targetZone == null || _selectedDefinition == null)
            {
                SetGhostVisibility(false);
                return;
            }

            bool hasValidSelection = true;

            if(!UpdateBuildablePosition(cachedRaycast.position))
                hasValidSelection = false;

            _placementValid = hasValidSelection;

            SetGhostVisibility(hasValidSelection);
            UpdateGhostPreviews(_placementPosition, _placementRotation);
        }

        public void SetGhostVisibility(bool newVisibility)
        {
            if (_ghostPreview == null)
                return;

            if (_ghostPreview.activeInHierarchy != newVisibility)
            {
                _ghostPreview.SetActive(newVisibility);
            }
        }

        private bool UpdateBuildablePosition(Vector3 position)
        {
            _placementPosition = position;
            return true;
        }

        private void UpdateGhostPreviews(Vector3 worldPosition, Quaternion rotation)
        {
            _ghostPreview.transform.position = worldPosition;
            _ghostPreview.transform.rotation = rotation;
            
            
        }

        public BuildableDefinition GetSelectedBuildable()
        {
            if (_selectedIndex < 0)
                return null;

            switch (_buildableCategory)
            {
                case EBuildableCategory.Wall:
                    return _availableBuildableWalls[_selectedIndex];
                case EBuildableCategory.Floor:
                    return _availableBuildableFloors[_selectedIndex];
                case EBuildableCategory.Feature:
                    return _availableBuildableFeatures[_selectedIndex];
                default:
                    return null;
            }
        }

        public IReadOnlyList<BuildableDefinition> ActiveBuildables
        {
            get
            {
                switch (_buildableCategory)
                {
                    case EBuildableCategory.Wall:
                        return _availableBuildableWalls;
                    case EBuildableCategory.Floor:
                        return _availableBuildableFloors;
                    case EBuildableCategory.Feature:
                        return _availableBuildableFeatures;

                    case EBuildableCategory.Roof:
                    case EBuildableCategory.Interior:
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
        Feature,
        Roof,
        Interior,
        Interactable,
    }
}
