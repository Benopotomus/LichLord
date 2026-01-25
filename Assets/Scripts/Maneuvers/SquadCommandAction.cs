
using Fusion;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "SwapWeaponAction", menuName = "LichLord/Maneuvers/SquadCommandAction", order = 1)]
    public class SquadCommandAction : ManeuverActionDefinition
    {
        [SerializeField]
        private int _squadId = 0;
        public int SquadId => _squadId;

        [SerializeField]
        private float _sensitivity = 5.0f;
        public float Sensitivity => _sensitivity;

        public override void Execute(PlayerCharacter pc, NetworkRunner runner)
        {
            pc.CameraController.LockAiming = true;
            Vector3 targetPos = pc.Context.Camera.CachedRaycastHit.position;
            pc.Commander.SetCommandPosition(_squadId, targetPos);
            pc.Commander.SetCommandRotation(_squadId, (targetPos - pc.Position).normalized);
        }

        public override void Sustain(PlayerCharacter pc, NetworkRunner runner)
        {
            pc.CameraController.LockAiming = true;
            var lookDelta = pc.Input.CurrentInput.LookDelta;
            pc.Commander.ModifyCommandRotation(_squadId, lookDelta.y * Sensitivity);
        }

        public override void EndExecute(PlayerCharacter pc, NetworkRunner runner)
        {
            pc.CameraController.LockAiming = false;
        }
    }
}
