using UnityEngine;
using Cinemachine;

namespace LichLord
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineVirtualCamera thirdPersonCam;
        [SerializeField] private CinemachineVirtualCamera firstPersonCam;

        private bool isFirstPerson = false;

        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
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
    }
}