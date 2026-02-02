
using Fusion;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "RecallCommandSquadsAction", menuName = "LichLord/Maneuvers/Actions/RecallCommandSquadsAction")]
    public class RecallCommandSquadsAction : ManeuverActionDefinition
    {
        public override void Execute(PlayerCharacter pc, NetworkRunner runner)
        {
            pc.Commander.RecallCommandSquads();
        }

        public override void Sustain(PlayerCharacter pc, NetworkRunner runner)
        {

        }

        public override void EndExecute(PlayerCharacter pc, NetworkRunner runner)
        {

        }
    }
}
