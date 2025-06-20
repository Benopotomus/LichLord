using Cinemachine;
using UnityEngine;

namespace LichLord
{
    public class SceneCamera : SceneService
    {
        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineVirtualCamera thirdPersonCam;
        [SerializeField] private CinemachineVirtualCamera firstPersonCam;

        [Header("Raycast Settings")]
        [SerializeField] private float _minRaycastDistance = 2.7f;
        [SerializeField] private float _maxRaycastDistance = 100f;
        [SerializeField] private LayerMask raycastLayerMask;
        private float sphereRadius = 0.1f; // Radius of the debug sphere

        private bool isFirstPerson = false;
        private FCachedRaycast _cachedRaycastHit; // Store last hit/max range point
        public FCachedRaycast CachedRaycastHit => _cachedRaycastHit;

        private bool lastRaycastHit; // True if last raycast hit something

        protected override void OnInitialize()
        {
            Camera camera = Context.Runner.SimulationUnityScene.FindMainCamera();
            if (camera != null)
            {
                camera.gameObject.SetActive(true);
            }

            // Set initial camera view
            SetCameraView(isFirstPerson);
        }

        public void SetCameraTargets(Transform firstPersonTarget, Transform thirdPersonFollowTarget)
        {
            if (thirdPersonCam != null)
            {
                if (thirdPersonFollowTarget != null)
                {
                    thirdPersonCam.Follow = thirdPersonFollowTarget;
                    thirdPersonCam.LookAt = thirdPersonFollowTarget;
                }
            }

            if (firstPersonCam != null)
            {
                if (firstPersonTarget != null)
                {
                    firstPersonCam.Follow = firstPersonTarget;
                    thirdPersonCam.LookAt = firstPersonTarget;
                }
                else
                {
                    Debug.LogWarning("[CameraManager] First person camera follow target not assigned.");
                }
            }
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

            if(localPlayerCreature != null ) 
                RaycastFromCameraCenter(localPlayerCreature.gameObject);


        }

        /// <summary>
        /// Performs a raycast from the center of the active camera
        /// </summary>
        /// <param name="hitInfo">Raycast hit information if something was hit</param>
        /// <returns>The hit point if the raycast hit something, otherwise the point at max range</returns>
        public void RaycastFromCameraCenter(GameObject ignoredObject)
        {
            // Get the active camera's Cinemachine brain
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[CameraManager] Main camera not found!");
                _cachedRaycastHit.raycastHit = new RaycastHit();
                _cachedRaycastHit.position = Vector3.zero;
            }

            float minDistance = isFirstPerson ? 0 : _minRaycastDistance;

            Transform cameraTransform = mainCamera.transform;
            // Calculate ray from camera center
            Vector3 rayOrigin = cameraTransform.position + (cameraTransform.forward * minDistance);
            Vector3 rayDirection = cameraTransform.forward;

            // Perform raycast with all hits
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, _maxRaycastDistance, raycastLayerMask);

            // Find the closest valid hit, ignoring the specified object
            RaycastHit closestHit = new RaycastHit();
            float closestDistance = float.MaxValue;
            bool foundValidHit = false;

            foreach (var hit in hits)
            {
                // Skip hits on the ignored object or its children
                if (ignoredObject != null && (hit.collider.gameObject == ignoredObject || hit.collider.transform.IsChildOf(ignoredObject.transform)))
                    continue;

                if (hit.distance < closestDistance)
                {
                    closestHit = hit;
                    closestDistance = hit.distance;
                    foundValidHit = true;
                }
            }

            if (foundValidHit)
            {
                _cachedRaycastHit.raycastHit = closestHit;
                _cachedRaycastHit.position = closestHit.point;
                lastRaycastHit = true;
                return;
            }

            // Return point at max distance if no valid hit
            Vector3 maxRangePoint = rayOrigin + rayDirection * _maxRaycastDistance;
            _cachedRaycastHit.raycastHit = new RaycastHit();
            _cachedRaycastHit.position = maxRangePoint;
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

    public struct FCachedRaycast
    { 
        public RaycastHit raycastHit;
        public Vector3 position;
    }
}