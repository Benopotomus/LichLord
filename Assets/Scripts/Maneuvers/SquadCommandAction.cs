
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

        public override void Execute(PlayerCharacter pc, NetworkRunner runner)
        {
            Vector3 targetPos = pc.Context.Camera.CachedRaycastHit.position;
            pc.Commander.SetCommandPosition(_squadId, targetPos);
        }
    }
}
