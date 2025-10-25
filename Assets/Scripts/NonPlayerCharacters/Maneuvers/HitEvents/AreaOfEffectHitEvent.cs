
using DWD.Utility.Loading;
using LichLord.Projectiles;
using LichLord.World;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "AreaOfEffectHitEvent", menuName = "LichLord/NonPlayerCharacters/HitEvents/AreaOfEffectHitEvent")]
    public class AreaOfEffectHitEvent : NonPlayerCharacterManeuverHitEvent
    {
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        private BundleObject _hitEffect;
        public BundleObject HitEffect => _hitEffect;

        [SerializeField]
        private EMuzzle _muzzle;
        public EMuzzle Muzzle => _muzzle;

        [SerializeField]
        private float _aoeRadius;
        public float AoeRadius => _aoeRadius;

        [SerializeField]
        private float _aoeHeight;
        public float AoeHeight => _aoeHeight;

        [SerializeField]
        private int _damage;
        public int Damage => _damage;

        [SerializeField]
        protected LayerMask _hitCollisionLayer;
        public LayerMask HitCollisionLayer => _hitCollisionLayer;

        public override void Execute(NonPlayerCharacter npc,
            NonPlayerCharacterManeuverDefinition definition,
            IChunkTrackable target)
        {
            Vector3 muzzlePosition = npc.Weapons.GetMuzzlePosition(Muzzle);

            npc.Context.VFXManager.SpawnVisualEffect(muzzlePosition, Quaternion.identity, HitEffect);

            Collider[] hitColliders = Physics.OverlapSphere(muzzlePosition, _aoeRadius, _hitCollisionLayer);

            foreach (Collider collider in hitColliders)
            {
                IHitTarget hitTarget = null;

                if (collider.tag == "Hurtbox")
                {
                    HurtboxOwner hitboxOwnerComp = collider.GetComponent<HurtboxOwner>();
                    if (hitboxOwnerComp == null)
                        continue;

                    hitTarget = hitboxOwnerComp.HitTarget;

                    if(!IsImpactObjectValid(npc, hitTarget))
                        continue;

                    ApplyHitToTarget(hitTarget, npc, Damage);
                }
            }
        }

        public static bool IsImpactObjectValid(NonPlayerCharacter npc, IHitTarget hitTarget)
        {
            if (npc == (object)hitTarget)
                return false;

            return true;
        }

        public void ApplyHitToTarget(IHitTarget hitTarget, NonPlayerCharacter npc, int damage)
        {
            if (hitTarget is PlayerCharacter targetPlayer)
            {
                targetPlayer.RPC_TakeHitNPC(0, damage);
                return;
            }

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
