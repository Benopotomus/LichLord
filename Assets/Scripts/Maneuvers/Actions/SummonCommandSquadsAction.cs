
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

            // Limit the distance from the static position to 20 units
            // Then if its not on the terrian, raycast down from that point to hit the ground

            pc.Context.NonPlayerCharacterManager.RPC_SpawnCommandGroupsFromItems(
                position,
                pc.Inventory.GetSquadItemsAtIndex(0),
                pc.Inventory.GetSquadItemsAtIndex(1),
                pc.Inventory.GetSquadItemsAtIndex(2),
                pc.TeamID,
                (byte)pc.PlayerIndex);
        }

        public override void Sustain(PlayerCharacter pc, NetworkRunner runner)
        {

        }

        public override void EndExecute(PlayerCharacter pc, NetworkRunner runner)
        {

        }
    }
}
