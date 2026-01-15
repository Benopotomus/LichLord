using LichLord.Buildables;
using LichLord.Items;
using LichLord.Props;
using LichLord.World;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "LichLord/Maneuvers/NPCDepositManeuverDefinition", order = 1)]
    public class NonPlayerCharacterDepositManeuverDefinition : NonPlayerCharacterManeuverDefinition
    {
        public override EManeuverType ManeuverType => EManeuverType.Deposit;

        public override bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            // if my hands aren't full, can't deposit.
            var carriedItem = brainComponent.NPC.RuntimeState.GetCarriedItem();
            if(!carriedItem.IsValid()) 
                return false;

            if(!brainComponent.DepositTarget.HasTarget)
                return false;

            IChunkTrackable target = brainComponent.DepositTarget.Target;

            float distanceToTarget = Vector3.Distance(
            target.Position,
            brainComponent.NPC.CachedTransform.position);

            if (distanceToTarget < ValidTargetDistance.x ||
                distanceToTarget > ValidTargetDistance.y)
                return false;

            if (target is Stockpile stockpile)
            {
                int containerIndex = stockpile.RuntimeState.GetContainerIndex();

                if (containerIndex >= 0)
                {
                    ContainerManager containerManager = brainComponent.NPC.Context.ContainerManager;

                    var containerData = containerManager.GetContainerDataAtIndex(containerIndex);

                    if (containerManager.CanStackAndFitContainer(containerIndex, carriedItem))
                        return true;
                }
            }

            return false;
        }

      
    }

}
