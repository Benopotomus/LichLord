
using LichLord.Props;
using LichLord.World;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "HarvetHitEvent", menuName = "LichLord/NonPlayerCharacters/HitEvents/HarvetHitEvent")]
    public class HarvetHitEvent : NonPlayerCharacterManeuverHitEvent
    {
        public override void Execute(NonPlayerCharacter npc, 
            NonPlayerCharacterManeuverDefinition definition,
            IChunkTrackable target)
        {
            if (!npc.Replicator.HasStateAuthority)
                return;

            if (target is HarvestNode harvestNode)
                harvestNode.ProgressHarvest(npc);
            
           // npc.Brain.FindCurrentTargets();
        }
    }
}
