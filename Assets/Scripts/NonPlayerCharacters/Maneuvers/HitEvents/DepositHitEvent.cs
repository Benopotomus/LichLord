
using LichLord.Buildables;
using LichLord.World;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "DepositHitEvent", menuName = "LichLord/NonPlayerCharacters/HitEvents/DepositHitEvent")]
    public class DepositHitEvent : NonPlayerCharacterManeuverHitEvent
    {
        public override void Execute(NonPlayerCharacter npc,
            NonPlayerCharacterManeuverDefinition definition,
            IChunkTrackable target)
        {
            if (!npc.Replicator.HasStateAuthority)
                return;

            if (target is Stockpile stockpile)
                stockpile.DropOffCurrency(npc);
            
            npc.Brain.FindCurrentTargets();
        }
    }
}
