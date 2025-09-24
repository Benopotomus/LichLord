
using LichLord.World;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{
    public class UITooltipTracker : UIWidget
    {
        Dictionary<IChunkTrackable, UIFloatingTooltip> _tooltipWidgets = new Dictionary<IChunkTrackable, UIFloatingTooltip>();
        public List<UIFloatingTooltip> _freeWidgets = new List<UIFloatingTooltip>();

        [SerializeField] private UIFloatingTooltip _floatingTooltipPrefab; // Prefab for the UI widget
        [SerializeField] private Transform _widgetParent; // Parent transform for spawned widgets
        [SerializeField] private float _trackableDetectionRange = 75f; // Max distance for the box
        [SerializeField] private LayerMask _trackableLayerMask; // Layer mask for trackables
        [SerializeField] private float _updateInterval = 0.1f; // Update every 0.1 seconds
        [SerializeField] private Vector2 _boxHalfExtents = new Vector2(20f, 20f); // Box width/height (half-extents)
        [SerializeField] private int _maxColliders = 64; // Max colliders for OverlapBoxNonAlloc

        private IChunkTrackable _hoveredTrackable;
        private List<IChunkTrackable> _visibleTrackables = new List<IChunkTrackable>();
        private bool _isTabHeld = false;
        private float _lastUpdateTime = 0f;
        private Collider[] _colliderBuffer; // Reusable buffer for OverlapBoxNonAlloc

        public void Awake()
        {
            _colliderBuffer = new Collider[_maxColliders]; // Initialize buffer
        }

        protected override void OnTick()
        {
            base.OnTick();

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                //Debug.LogWarning("[UITooltipTracker] Main camera not found!");
                return;
            }

            var pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return;

            FGameplayInput input = pc.Input.CurrentInput;

            // Check if Tab key is held
            bool isTabHeld = input.ShowTooltips;
            if (isTabHeld != _isTabHeld)
            {
                _isTabHeld = isTabHeld;
                if (isTabHeld)
                {
                    UpdateVisibleTrackables();
                    _lastUpdateTime = Time.time;
                }
                else
                {
                    ClearVisibleTrackables();
                }
            }

            // Update visible trackables if Tab is held and interval has passed
            if (_isTabHeld && Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateVisibleTrackables();
                _lastUpdateTime = Time.time;
            }

            // Handle single-trackable hover logic (for raycast-based hovering)
            var currentTrackable = Context.Camera.CachedRaycastHit.trackable;
            if (_hoveredTrackable != currentTrackable)
            {
                if (_hoveredTrackable != null && !_visibleTrackables.Contains(_hoveredTrackable))
                {
                    OnUnhover(_hoveredTrackable);
                }

                if (currentTrackable != null && !_visibleTrackables.Contains(currentTrackable))
                {
                    OnHover(currentTrackable);
                }

                _hoveredTrackable = currentTrackable;
            }
        }

        private void UpdateVisibleTrackables()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            // Get current colliders in a box extending from the camera
            Vector3 cameraPosition = mainCamera.transform.position;
            Quaternion cameraRotation = mainCamera.transform.rotation;
            Vector3 boxCenter = cameraPosition + cameraRotation * Vector3.forward * (_trackableDetectionRange / 2f);
            Vector3 boxHalfExtents = new Vector3(_boxHalfExtents.x, _boxHalfExtents.y, _trackableDetectionRange / 2f);

            int colliderCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, _colliderBuffer, cameraRotation, _trackableLayerMask);

            // Create a new list of visible trackables
            List<IChunkTrackable> newVisibleTrackables = new List<IChunkTrackable>();
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

            for (int i = 0; i < colliderCount; i++)
            {
                Collider collider = _colliderBuffer[i];
                IChunkTrackable trackable = collider.GetComponentInParent<IChunkTrackable>();
                if (trackable == null)
                    continue;

                Bounds bounds = collider.bounds;
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                {
                    newVisibleTrackables.Add(trackable);
                }
            }

            // Update tooltips for added/removed trackables
            foreach (var trackable in newVisibleTrackables)
            {
                if (!_visibleTrackables.Contains(trackable) && !_tooltipWidgets.ContainsKey(trackable))
                {
                    OnHover(trackable);
                }
            }

            foreach (var trackable in _visibleTrackables)
            {
                if (!newVisibleTrackables.Contains(trackable) && trackable != _hoveredTrackable)
                {
                    OnUnhover(trackable);
                }
            }

            _visibleTrackables = newVisibleTrackables;
            //Debug.Log($"[UITooltipTracker] Visible trackables: {_visibleTrackables.Count}, Colliders checked: {colliderCount}");
        }

        private void ClearVisibleTrackables()
        {
            foreach (var trackable in _visibleTrackables)
            {
                if (trackable != _hoveredTrackable)
                {
                    OnUnhover(trackable);
                }
            }
            _visibleTrackables.Clear();
        }

        public void OnHover(IChunkTrackable trackableHovered)
        {
            if (!_tooltipWidgets.ContainsKey(trackableHovered))
            {
                UIFloatingTooltip widget;
                if (_freeWidgets.Count > 0)
                {
                    widget = _freeWidgets[0];
                    _freeWidgets.RemoveAt(0);
                }
                else
                {
                    widget = Instantiate(_floatingTooltipPrefab, _widgetParent);
                    AddChild(widget);
                }

                widget.SetTooltipTarget(trackableHovered);
                _tooltipWidgets[trackableHovered] = widget;
                //Debug.Log($"[UITooltipTracker] Showing tooltip for trackable: {trackableHovered}");
            }
        }

        public void OnUnhover(IChunkTrackable trackableUnhovered)
        {
            if (_tooltipWidgets.TryGetValue(trackableUnhovered, out var widget))
            {
                widget.SetTooltipTarget(null);
                _freeWidgets.Add(widget);
                _tooltipWidgets.Remove(trackableUnhovered);
                //Debug.Log($"[UITooltipTracker] Hiding tooltip for trackable: {trackableUnhovered}");
            }
        }

        private void OnDrawGizmos()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            // Visualize the overlap box
            Vector3 cameraPosition = mainCamera.transform.position;
            Quaternion cameraRotation = mainCamera.transform.rotation;
            Vector3 boxCenter = cameraPosition + cameraRotation * Vector3.forward * (_trackableDetectionRange / 2f);
            Vector3 boxHalfExtents = new Vector3(_boxHalfExtents.x, _boxHalfExtents.y, _trackableDetectionRange / 2f);

            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, cameraRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}