
using Fusion;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "SummonCommandSquadsAction", menuName = "LichLord/Maneuvers/Actions/SummonCommandSquadsAction")]
    public class SummonCommandSquadsAction : ManeuverActionDefinition
    {
        public override void Execute(PlayerCharacter pc, NetworkRunner runner)
        {
            FWorldPosition position = new FWorldPosition();
            position.CopyPosition(pc.Maneuvers.ManeuverTargetPosition);
            pc.Commander.SummonCommandSquads(position);
        }

        public override void Sustain(PlayerCharacter pc, NetworkRunner runner)
        {

        }

        public override void EndExecute(PlayerCharacter pc, NetworkRunner runner)
        {

        }
    }
}
