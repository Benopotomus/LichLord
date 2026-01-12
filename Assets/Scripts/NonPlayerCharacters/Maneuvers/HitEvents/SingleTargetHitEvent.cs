
using DWD.Utility.Loading;
using LichLord.World;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "SingleTargetHitEvent", menuName = "LichLord/NonPlayerCharacters/HitEvents/SingleTargetHitEvent")]
    public class SingleTargetHitEvent : NonPlayerCharacterManeuverHitEvent
    {
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        private BundleObject _hitEffect;
        public BundleObject HitEffect => _hitEffect;

        public override void Execute(NonPlayerCharacter npc, 
            NonPlayerCharacterManeuverDefinition definition, 
            IChunkTrackable target)
        {
            if (definition is not NonPlayerCharacterAttackManeuverDefinition attackManeuverDefinition)
                return;

            if(target is PlayerCharacter targetPlayer)
            {
                if (npc.Context.LocalPlayerCharacter == targetPlayer)
                {
                    float distance = Vector3.Distance(targetPlayer.CachedTransform.position, npc.CachedTransform.position);

                    if (distance < attackManeuverDefinition.AttackRange)
                    {
                        targetPlayer.RPC_TakeHitNPC(0, attackManeuverDefinition.Damage);
                    }
                }

                return;
            }

            var hitTarget = target as IHitTarget;

            if (hitTarget != null)
            {
                ApplyHitToTarget(hitTarget, npc, attackManeuverDefinition.Damage);
            }
        }

        public void ApplyHitToTarget(IHitTarget hitTarget, NonPlayerCharacter npc, int damage)
        {
            FDamageData damageData = new FDamageData();
            damageData.damageValue = damage;

            FHitUtilityData hit = new FHitUtilityData
            {
                instigator = npc,
                target = hitTarget,
                damageData = damageData,
                staggerRating = 0,
                knockbackStrength = 0,
                impactRotation = Quaternion.identity,
                impactPosition = Vector3.zero,
                tick = npc.Context.Runner.Tick,
            };

            HitUtility.ProcessHit(ref hit, npc.Context);
        }
    }
}
