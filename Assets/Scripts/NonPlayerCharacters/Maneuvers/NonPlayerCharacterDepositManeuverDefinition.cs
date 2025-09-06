using LichLord.Buildables;
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
            var carriedCurrency = brainComponent.NPC.RuntimeState.GetCarriedCurrencyType();
            if(carriedCurrency == ECurrencyType.None ) 
                return false;

            IChunkTrackable depositTarget = brainComponent.DepositTarget;
            if (depositTarget == null)
                return false;

            float distanceToTarget = Vector3.Distance(
            depositTarget.Position,
            brainComponent.NPC.CachedTransform.position);

            if (distanceToTarget < ValidTargetDistance.x ||
                distanceToTarget > ValidTargetDistance.y)
                return false;

            if (depositTarget is Stockpile stockpile)
            {
                int stockpileIndex = stockpile.RuntimeState.GetStockpileIndex();

                if (stockpileIndex >= 0)
                {
                    var currencyAmount = brainComponent.NPC.RuntimeState.GetCarriedCurrencyAmount();

                    var stockpileData = brainComponent.NPC.Context.ContainerManager.GetStockPile(stockpileIndex);

                    if (stockpileData.CanFit(carriedCurrency, currencyAmount))
                        return true;
                }
            }

            return false;
        }
    }

}
