using Cinemachine;
using LichLord.Buildables;
using LichLord.World;
using System;
using UnityEngine;
using UnityEngine.UI; // For UI elements

namespace LichLord
{
    public class SceneCamera : SceneService
    {
        [SerializeField] private Transform _skydomeTransform;

        [SerializeField]
        public Transform _cameraFollowTarget;

        public Transform followTransform;

        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineVirtualCamera thirdPersonCam;
        [SerializeField] private CinemachineVirtualCamera firstPersonCam;

        [Header("Raycast Settings")]
        [SerializeField] private float _minRaycastDistance = 2.7f;
        [SerializeField] private float _maxRaycastDistance = 100f;
        [SerializeField] private LayerMask raycastLayerMask;
        [SerializeField] private LayerMask _buildableZoneLayerMask;
        [SerializeField] private LayerMask _trackableLayerMask;
        [SerializeField] private LayerMask _interactableLayerMask;

        [SerializeField] private Image reticle; // Reference to the reticle UI element

        private float sphereRadius = 0.1f; // Radius of the debug sphere

        private bool isFirstPerson = false;

        [SerializeField]
        private FCachedRaycast _cachedRaycastHit; // Store last hit/max range point
        public FCachedRaycast CachedRaycastHit => _cachedRaycastHit;

        private bool lastRaycastHit; // True if last raycast hit something
        
        [Header("Camera Shake")]
        [SerializeField] private float _defaultShakeDuration = 0.3f;
        [SerializeField] private float _defaultShakeAmplitude = 1.5f;
        [SerializeField] private float _defaultShakeFrequency = 12f;

        private Coroutine _shakeRoutine;
        private CinemachineBasicMultiChannelPerlin _thirdPersonNoise;
        private CinemachineBasicMultiChannelPerlin _firstPersonNoise;

        protected override void OnInitialize()
        {
            Camera camera = Context.Runner.SimulationUnityScene.FindMainCamera();
            if (camera != null)
            {
                camera.gameObject.SetActive(true);
            }

            // Set initial camera view
            SetCameraView(isFirstPerson);

            thirdPersonCam.Follow = _cameraFollowTarget;
            thirdPersonCam.LookAt = _cameraFollowTarget;
            firstPersonCam.Follow = _cameraFollowTarget;
            firstPersonCam.LookAt = _cameraFollowTarget;

            // Ensure reticle is visible if assigned
            if (reticle != null)
            {
                reticle.gameObject.SetActive(true);
            }


        }

        private void EnsureNoiseSettings(CinemachineBasicMultiChannelPerlin noise)
        {
            if (noise == null) return;

            // Option 1: Use a NoiseSettings asset you created in the editor (recommended)
            // Drag it onto the field below in the inspector
            if (noise.m_NoiseProfile == null)
            {
                // Try to load the built-in basic profile that ships with Cinemachine
                var basic = Resources.Load<Cinemachine.NoiseSettings>("CinemachineBasicNoise");
                if (basic != null)
                    noise.m_NoiseProfile = basic;
                else
                {
                    // Fallback: create a minimal one at runtime
                    noise.m_NoiseProfile = CreateMinimalNoiseSettings();
                }
            }
        }

        private Cinemachine.NoiseSettings CreateMinimalNoiseSettings()
        {
            var settings = ScriptableObject.CreateInstance<Cinemachine.NoiseSettings>();
            settings.name = "RuntimeMinimalShake";

            // ----- POSITION (XYZ) -----
            settings.PositionNoise = new Cinemachine.NoiseSettings.TransformNoiseParams[]
            {
        new Cinemachine.NoiseSettings.TransformNoiseParams
        {
            X = new Cinemachine.NoiseSettings.NoiseParams { Amplitude = 1f, Frequency = 1f },
            Y = new Cinemachine.NoiseSettings.NoiseParams { Amplitude = 1f, Frequency = 1f },
            Z = new Cinemachine.NoiseSettings.NoiseParams { Amplitude = 1f, Frequency = 1f }
        }
            };

            // ----- ROTATION (XYZ) -----
            settings.OrientationNoise = new Cinemachine.NoiseSettings.TransformNoiseParams[]
            {
        new Cinemachine.NoiseSettings.TransformNoiseParams
        {
            X = new Cinemachine.NoiseSettings.NoiseParams { Amplitude = 1f, Frequency = 1f },
            Y = new Cinemachine.NoiseSettings.NoiseParams { Amplitude = 1f, Frequency = 1f },
            Z = new Cinemachine.NoiseSettings.NoiseParams { Amplitude = 1f, Frequency = 1f }
        }
            };

            return settings;
        }

        public void ModifyCameraTargetRotation(Quaternion newRotation)
        {
            _cameraFollowTarget.rotation = newRotation;
        }

        public void SetCameraFollow(Transform transform)
        {
            followTransform = transform;
        }

        public void SetCameraView(bool firstPerson)
        {
            isFirstPerson = firstPerson;

            if (firstPersonCam != null && thirdPersonCam != null)
            {
                firstPersonCam.Priority = firstPerson ? 20 : 10;
                thirdPersonCam.Priority = firstPerson ? 10 : 20;

                Debug.Log($"[CameraManager] Switched to {(firstPerson ? "First Person" : "Third Person")} view");
            }
            else
            {
                Debug.LogError("[CameraManager] One or both cameras not assigned!");
            }
        }

        protected override void OnTick()
        {
            PlayerCharacter localPlayerCreature = Context.LocalPlayerCharacter;

            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                _cachedRaycastHit.Clear();
                return;
            }

            Transform cameraTransform = mainCamera.transform;
            Vector3 cameraPosition = mainCamera.transform.position;

            // Create a ray from the center of the camera viewport (slightly upward)
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            // Offset the ray origin forward in third person mode
            float minDistance = isFirstPerson ? 0f : _minRaycastDistance;
            Vector3 rayOrigin = ray.origin + ray.direction * minDistance;
            Vector3 rayDirection = ray.direction;

            _skydomeTransform.position = cameraPosition;

            if (localPlayerCreature == null)
                return;

            RaycastFromCameraCenter(rayOrigin, rayDirection, localPlayerCreature.gameObject);
            CheckOverlapsAtOriginNonAlloc(rayOrigin, localPlayerCreature.gameObject);

            if (followTransform != null)
                _cameraFollowTarget.position = followTransform.position;

            // Update reticle position
            UpdateReticlePosition(rayOrigin, rayDirection);
        }

        private Collider[] _overlapBuffer = new Collider[16]; // Reuse this buffer

        private void CheckOverlapsAtOriginNonAlloc(Vector3 origin, GameObject ignoredObject)
        {
            // Physics.OverlapSphereNonAlloc returns the number of hits
            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                0.05f,
                _overlapBuffer,
                _buildableZoneLayerMask | _interactableLayerMask | _trackableLayerMask,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < hitCount; i++)
            {
                var col = _overlapBuffer[i];

                if (ignoredObject != null && (col.gameObject == ignoredObject || col.transform.IsChildOf(ignoredObject.transform)))
                    continue;

                int colLayerBit = 1 << col.gameObject.layer;

                // Buildable
                if ((_buildableZoneLayerMask.value & colLayerBit) != 0)
                {
                    BuildableZone bz = col.GetComponent<BuildableZone>();
                    if (bz != null)
                        _cachedRaycastHit.buildableZone = bz;
                }

                // Interactable
                if ((_interactableLayerMask.value & colLayerBit) != 0)
                {
                    InteractableComponent interactable = col.GetComponent<InteractableComponent>();
                    if (interactable != null && interactable.IsPotentialInteractor(Context.LocalPlayerCharacter.Interactor))
                        _cachedRaycastHit.interactable = interactable;
                }

                // Trackable
                if ((_trackableLayerMask.value & colLayerBit) != 0)
                {
                    IChunkTrackable trackable = col.GetComponentInParent<IChunkTrackable>();
                    if (trackable != null)
                        _cachedRaycastHit.trackable = trackable;
                }
            }
        }

        public void RaycastFromCameraCenter(Vector3 rayOrigin, Vector3 rayDirection, GameObject ignoredObject)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[CameraManager] Main camera not found!");
                _cachedRaycastHit = new FCachedRaycast();
                return;
            }

            LayerMask combinedMask = raycastLayerMask
                | _buildableZoneLayerMask
                | _trackableLayerMask
                | _interactableLayerMask;

            // Reset cached results
            _cachedRaycastHit.buildableZone = null;
            _cachedRaycastHit.trackable = null;
            _cachedRaycastHit.interactable = null;

            float closestWorldHit = float.MaxValue;
            float closestInteractableDist = float.MaxValue;
            float closestBuildableDist = float.MaxValue;
            float closestTrackableDist = float.MaxValue;

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, _maxRaycastDistance, combinedMask, QueryTriggerInteraction.Collide);

            RaycastHit closestHit = new RaycastHit();
            bool foundValidWorldHit = false;

            foreach (var hit in hits)
            {
                // Skip ignored object and children
                if (ignoredObject != null && (hit.collider.gameObject == ignoredObject || hit.collider.transform.IsChildOf(ignoredObject.transform)))
                    continue;

                float dist = hit.distance;

                // Trackables
                if (((1 << hit.collider.gameObject.layer) & _trackableLayerMask) != 0)
                {
                    IChunkTrackable trackable = hit.collider.GetComponentInParent<IChunkTrackable>();
                    if (trackable != null && dist < closestTrackableDist)
                    {
                        closestTrackableDist = dist;
                        _cachedRaycastHit.trackable = trackable;
                    }
                }

                // Buildables
                if (((1 << hit.collider.gameObject.layer) & _buildableZoneLayerMask) != 0)
                {
                    BuildableZone bz = hit.collider.GetComponent<BuildableZone>();
                    if (bz != null && dist < closestBuildableDist)
                    {
                        closestBuildableDist = dist;
                        _cachedRaycastHit.buildableZone = bz;
                    }
                }

                // Interactables
                if (((1 << hit.collider.gameObject.layer) & _interactableLayerMask) != 0)
                {
                    InteractableComponent interactable = hit.collider.GetComponent<InteractableComponent>();
                    if (interactable != null && interactable.IsPotentialInteractor(Context.LocalPlayerCharacter.Interactor))
                    {
                        if (dist < closestInteractableDist)
                        {
                            closestInteractableDist = dist;
                            _cachedRaycastHit.interactable = interactable;
                        }
                    }
                }

                // Generic "world" hit (e.g., terrain, wall, etc.)
                if (((1 << hit.collider.gameObject.layer) & raycastLayerMask) != 0)
                {
                    if (dist < closestWorldHit)
                    {
                        closestWorldHit = dist;
                        closestHit = hit;
                        foundValidWorldHit = true;
                    }
                }
            }

            if (foundValidWorldHit)
            {
                _cachedRaycastHit.raycastHit = closestHit;
                _cachedRaycastHit.position = closestHit.point;
                lastRaycastHit = true;
            }
            else
            {
                Vector3 maxRangePoint = rayOrigin + rayDirection * _maxRaycastDistance;
                _cachedRaycastHit.raycastHit = new RaycastHit();
                _cachedRaycastHit.position = maxRangePoint;
                lastRaycastHit = false;
            }
        }

        private void UpdateReticlePosition(Vector3 rayOrigin, Vector3 rayDirection)
        {
            if (reticle == null || Camera.main == null)
                return;

            Camera mainCamera = Camera.main;

            // Project the raycast position to screen space
            Vector3 worldPosition = _cachedRaycastHit.position;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            // Clamp to viewport to keep reticle on screen
            Rect viewportRect = new Rect(0, 0, Screen.width, Screen.height);
            screenPosition.x = Mathf.Clamp(screenPosition.x, viewportRect.xMin, viewportRect.xMax);
            screenPosition.y = Mathf.Clamp(screenPosition.y, viewportRect.yMin, viewportRect.yMax);

            // Set reticle position (convert to UI space, assuming Canvas is Screen Space - Overlay)
            RectTransform reticleRectTransform = reticle.GetComponent<RectTransform>();
            if (reticleRectTransform != null)
            {
                reticleRectTransform.position = screenPosition;
            }
        }

        private void OnDrawGizmos()
        {
            // Draw sphere at the last raycast point
            if (_cachedRaycastHit.position != Vector3.zero)
            {
                Gizmos.color = lastRaycastHit ? Color.red : Color.green;
                Gizmos.DrawWireSphere(_cachedRaycastHit.position, sphereRadius);
            }
        }
    }

    [Serializable]
    public struct FCachedRaycast
    {
        public RaycastHit raycastHit;
        public Vector3 position;
        public BuildableZone buildableZone;
        public IChunkTrackable trackable;
        public InteractableComponent interactable;

        public void Clear()
        {
            buildableZone = null;
            trackable = null;
            interactable = null;
        }
    }
}