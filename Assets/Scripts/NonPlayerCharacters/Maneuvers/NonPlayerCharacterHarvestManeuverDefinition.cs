using LichLord.Props;
using LichLord.World;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "LichLord/Maneuvers/NPCHarvestManeuverDefinition", order = 1)]
    public class NonPlayerCharacterHarvestManeuverDefinition : NonPlayerCharacterManeuverDefinition
    {
        public override EManeuverType ManeuverType => EManeuverType.Harvest;

        public override bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            // If my hands are full I can't harvest
            var carriedItem = brainComponent.NPC.RuntimeState.GetCarriedItem();
            if (carriedItem.IsValid())
                return false;

            IChunkTrackable harvestTarget = brainComponent.HarvestTarget;
            if (harvestTarget == null)
                return false;

            float distanceToTarget = Vector3.Distance(
            harvestTarget.Position,
            brainComponent.NPC.CachedTransform.position);

            if (distanceToTarget < ValidTargetDistance.x ||
                distanceToTarget > ValidTargetDistance.y)
                return false;

            if (harvestTarget is HarvestNode harvestNode)
            {
                if(harvestNode.RuntimeState.GetHarvestPoints() > 0)
                    return true;
            }

            return false;
        }
    }

}
