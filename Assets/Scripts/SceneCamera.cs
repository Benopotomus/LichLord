using UnityEngine;
using Cinemachine;

namespace LichLord
{
    public class SceneCamera : SceneService
    {
        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineVirtualCamera thirdPersonCam;
        [SerializeField] private CinemachineVirtualCamera firstPersonCam;

        [Header("Raycast Settings")]
        [SerializeField] private float maxRaycastDistance = 100f;
        [SerializeField] private LayerMask raycastLayerMask;
        [SerializeField] private float sphereRadius = 0.25f; // Radius of the debug sphere

        private bool isFirstPerson = false;
        private Vector3 lastRaycastPoint; // Store last hit/max range point
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

        /// <summary>
        /// Performs a raycast from the center of the active camera
        /// </summary>
        /// <param name="hitInfo">Raycast hit information if something was hit</param>
        /// <returns>The hit point if the raycast hit something, otherwise the point at max range</returns>
        public Vector3 RaycastFromCameraCenter(GameObject ignoredObject, out RaycastHit hitInfo)
        {
            // Get the active camera's Cinemachine brain
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[CameraManager] Main camera not found!");
                hitInfo = new RaycastHit();
                lastRaycastPoint = Vector3.zero;
                lastRaycastHit = false;
                return Vector3.zero;
            }

            // Calculate ray from camera center
            Vector3 rayOrigin = mainCamera.transform.position;
            Vector3 rayDirection = mainCamera.transform.forward;

            // Perform raycast with all hits
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, maxRaycastDistance, raycastLayerMask);

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
                hitInfo = closestHit;
                lastRaycastPoint = hitInfo.point;
                lastRaycastHit = true;
                return hitInfo.point;
            }

            // Return point at max distance if no valid hit
            Vector3 maxRangePoint = rayOrigin + rayDirection * maxRaycastDistance;
            hitInfo = new RaycastHit();
            lastRaycastPoint = maxRangePoint;
            lastRaycastHit = false;
            return maxRangePoint;
        }

        private void OnDrawGizmos()
        {
            // Draw sphere at the last raycast point
            if (lastRaycastPoint != Vector3.zero)
            {
                Gizmos.color = lastRaycastHit ? Color.red : Color.green;
                Gizmos.DrawWireSphere(lastRaycastPoint, sphereRadius);
            }
        }
    }
}