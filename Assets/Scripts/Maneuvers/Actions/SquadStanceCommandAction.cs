
using Fusion;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "SquadStanceCommandAction", menuName = "LichLord/Maneuvers/SquadStanceCommandAction", order = 1)]
    public class SquadStanceCommandAction : ManeuverActionDefinition
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
            pc.Commander.SetModifyingStance(_squadId, true);
        }

        public override void Sustain(PlayerCharacter pc, NetworkRunner runner)
        {
            pc.CameraController.LockAiming = true;
            var lookDelta = pc.Input.CurrentInput.LookDelta;
            pc.Commander.ModifyDesiredCommandStance(_squadId, lookDelta.y * Sensitivity);
        }

        public override void EndExecute(PlayerCharacter pc, NetworkRunner runner)
        {
            pc.CameraController.LockAiming = false;
            pc.Commander.ConfirmDesiredStance(_squadId);
            pc.Commander.SetModifyingStance(_squadId, false);
        }
    }
}
