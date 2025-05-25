using UnityEngine;
using Cinemachine;
using Fusion.Addons.SimpleKCC;
using Fusion;

namespace LichLord
{
    public class PlayerCameraController : NetworkBehaviour
    {
        [Header("Cinemachine Cameras")]
        public Transform firstPersonFollowTarget;
        public Transform thirdPersonFollowTarget;

        [Header("Shoulder Settings (Third Person Only)")]
        public float shoulderOffset = 0.5f;
        public float cameraDistance = 4f;
        public float cameraHeight = 1.5f;

        private PlayerCharacterInput _playerInput;

        private bool isFirstPerson = false;

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority)
            {
                if (CameraManager.Instance != null)
                {
                    CameraManager.Instance.SetCameraTargets(
                        firstPersonFollowTarget,
                        thirdPersonFollowTarget
                    );
                }
                else
                {
                    Debug.LogError("[PlayerCameraController] CameraManager instance not found!");
                }
            }
        }

        void Start()
        {
            _playerInput = GetComponent<PlayerCharacterInput>();
            if (!_playerInput)
                Debug.LogError("[PlayerCameraController] Missing PlayerCharacterInput component.");

        }

        void LateUpdate()
        {
            if (!HasStateAuthority)
                return;

            if (_playerInput == null) return;

            if (_playerInput.CurrentInput.ToggleCameraView)
            {
                isFirstPerson = !isFirstPerson;
                CameraManager.Instance.SetCameraView(isFirstPerson);
            }

            firstPersonFollowTarget.rotation = Quaternion.Euler(_playerInput.CurrentInput.LookRotation);
            thirdPersonFollowTarget.rotation = Quaternion.Euler(_playerInput.CurrentInput.LookRotation);

        }

    }
}