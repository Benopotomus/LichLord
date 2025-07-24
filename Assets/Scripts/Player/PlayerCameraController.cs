using UnityEngine;

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
                Context.Camera.SetCameraFollow(
                        thirdPersonFollowTarget);
            }
        }

        Vector2 _lastEuler = Vector2.zero;
        public void ProcessInput(ref FGameplayInput input)
        {
            if (!HasStateAuthority)
                return;

            Vector2 lookDelta = input.LookDelta;
            Vector2 newEuler = _lastEuler + lookDelta;

            // Clamp the X rotation (pitch) to -60/60 degrees
            newEuler.x = Mathf.Clamp(newEuler.x, -60f, 60f);

            Context.Camera.ModifyCameraTargetRotation(Quaternion.Euler(newEuler));
            
            _lastEuler = newEuler;

            if (input.ToggleCameraView)
            {
                isFirstPerson = !isFirstPerson;

                var followTarget = isFirstPerson ? firstPersonFollowTarget : thirdPersonFollowTarget; ;
                Context.Camera.SetCameraView(isFirstPerson);
                Context.Camera.SetCameraFollow(followTarget);
            }
        }

    }
}