using Cinemachine;
using LichLord.Buildables;
using UnityEngine;

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
        private float sphereRadius = 0.1f; // Radius of the debug sphere

        private bool isFirstPerson = false;
        private FCachedRaycast _cachedRaycastHit; // Store last hit/max range point
        public FCachedRaycast CachedRaycastHit => _cachedRaycastHit;

        [SerializeField] private LayerMask _buildableZoneLayer;


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

            thirdPersonCam.Follow = _cameraFollowTarget;
            thirdPersonCam.LookAt = _cameraFollowTarget;
            firstPersonCam.Follow = _cameraFollowTarget;
            firstPersonCam.LookAt = _cameraFollowTarget;
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

            if(localPlayerCreature != null ) 
                RaycastFromCameraCenter(localPlayerCreature.gameObject);

            Camera mainCamera = Camera.main;
            _skydomeTransform.position = mainCamera.transform.position;

            if(followTransform != null)
                _cameraFollowTarget.position = followTransform.position;
        }

        private void LateUpdate()
        {

        }

        public void RaycastFromCameraCenter(GameObject ignoredObject)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[CameraManager] Main camera not found!");
                _cachedRaycastHit = new FCachedRaycast();
                return;
            }

            float minDistance = isFirstPerson ? 0 : _minRaycastDistance;
            Transform cameraTransform = mainCamera.transform;

            Vector3 rayOrigin = cameraTransform.position + (cameraTransform.forward * minDistance);
            Vector3 rayDirection = cameraTransform.forward;

            LayerMask combinedMask = raycastLayerMask | _buildableZoneLayer;

            // Reset cached buildable zone
            _cachedRaycastHit.buildableZone = null;

            // OverlapSphere to detect if inside a buildable zone trigger
            Collider[] overlappingColliders = Physics.OverlapSphere(rayOrigin, 0.1f, _buildableZoneLayer);
            foreach (var collider in overlappingColliders)
            {
                BuildableZone bz = collider.GetComponent<BuildableZone>();
                if (bz != null)
                {
                    _cachedRaycastHit.buildableZone = bz;
                    break;
                }
            }

            // RaycastAll including triggers
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, _maxRaycastDistance, combinedMask, QueryTriggerInteraction.Collide);

            RaycastHit closestHit = new RaycastHit();
            float closestDistance = float.MaxValue;
            bool foundValidHit = false;

            foreach (var hit in hits)
            {
                // Skip ignored object and children
                if (ignoredObject != null && (hit.collider.gameObject == ignoredObject || hit.collider.transform.IsChildOf(ignoredObject.transform)))
                    continue;

                // Check if this hit is on buildable zone layer
                if (((1 << hit.collider.gameObject.layer) & _buildableZoneLayer) != 0)
                {
                    BuildableZone bz = hit.collider.GetComponent<BuildableZone>();
                    if (bz != null)
                    {
                        _cachedRaycastHit.buildableZone = bz;
                        // Note: Do NOT affect closest hit with this
                        // Just keep the buildable zone reference here
                    }
                }

                // Check if this hit is in the raycastLayerMask (excluding buildable zone layer)
                if (((1 << hit.collider.gameObject.layer) & raycastLayerMask) != 0)
                {
                    if (hit.distance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hit.distance;
                        foundValidHit = true;
                    }
                }
            }

            if (foundValidHit)
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
        public BuildableZone buildableZone;
    }
}