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

        private bool isFirstPerson = false;

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
        /// <returns>True if the raycast hit something, false otherwise</returns>
        public Vector3 RaycastFromCameraCenter(out RaycastHit hitInfo)
        {
            // Get the active camera's Cinemachine brain
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[CameraManager] Main camera not found!");
                hitInfo = new RaycastHit();
                return Vector3.zero;
            }

            // Calculate ray from camera center
            Vector3 rayOrigin = mainCamera.transform.position;
            Vector3 rayDirection = mainCamera.transform.forward;

            // Perform raycast
            if (Physics.Raycast(rayOrigin, rayDirection, out hitInfo, maxRaycastDistance, raycastLayerMask))
            {
                Debug.DrawRay(rayOrigin, rayDirection * hitInfo.distance, Color.red, 1f);
                return hitInfo.point;
            }

            // Return point at max distance if no hit
            Vector3 maxRangePoint = rayOrigin + rayDirection * maxRaycastDistance;
            Debug.DrawRay(rayOrigin, rayDirection * maxRaycastDistance, Color.green, 1f);
            return maxRangePoint;
        }
    }
}