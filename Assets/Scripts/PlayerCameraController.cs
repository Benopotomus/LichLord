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

        public void ProcessInput(GameplayInput input)
        {
            if (!HasStateAuthority)
                return;

            if (input.ToggleCameraView)
            {
                isFirstPerson = !isFirstPerson;
                CameraManager.Instance.SetCameraView(isFirstPerson);
            }

            firstPersonFollowTarget.rotation = Quaternion.Euler(input.LookRotation);
            thirdPersonFollowTarget.rotation = Quaternion.Euler(input.LookRotation);

        }

    }
}