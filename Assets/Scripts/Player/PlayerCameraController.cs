using UnityEngine;
using Cinemachine;
using Fusion.Addons.SimpleKCC;
using Fusion;

namespace LichLord
{
    public class PlayerCameraController : ContextBehaviour
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
                Context.Camera.SetCameraTargets(
                        firstPersonFollowTarget,
                        thirdPersonFollowTarget);
            }
        }

        public void ProcessInput(FGameplayInput input)
        {
            if (!HasStateAuthority)
                return;

            firstPersonFollowTarget.rotation = Quaternion.Euler(input.LookRotation);
            thirdPersonFollowTarget.rotation = Quaternion.Euler(input.LookRotation);

            if (input.ToggleCameraView)
            {
                isFirstPerson = !isFirstPerson;
                Context.Camera.SetCameraView(isFirstPerson);
            }

        }

    }
}