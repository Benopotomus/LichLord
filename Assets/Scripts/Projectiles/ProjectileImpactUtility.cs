
namespace LichLord.Projectiles
{
    using UnityEngine;
    
    public static class ProjectileImpactUtility
    {
        public static void HandleCollisionHitActor(Projectile projectile,
            ref FProjectileData data,
            ref FProjectileCollisionEvent collisionEvent,
            int tick)
        {
            // Check if Reflect
            //if (HandleReflect(projectile, ref data, ref collisionEvent, tick))
            //    return;

            // Apply GPE 
            //SpawnGameplayEffectsForTarget(projectile, ref data, ref collisionEvent, tick);

            // Apply Damage
            ApplyHitToTarget(projectile, ref data, ref collisionEvent, tick);
        }

        public static void ApplyHitToTarget(Projectile projectile,
            ref FProjectileData data,
            ref FProjectileCollisionEvent collisionEvent,
            int tick)
        {
            //ref FDamagePotential damagePotential = ref projectile.Payload.damagePotential;
            IHitTarget hitActor = collisionEvent.hitTarget;
            //FDamageData damageData = DamageUtility.GetDamageToActor(ref damagePotential, hitActor);
            FDamageData damageData = new FDamageData();
            damageData.damageValue = projectile.Payload.damagePotential.DamageValue;

            FHitUtilityData hit = new FHitUtilityData
            {
                instigator = projectile.Instigator,
                target = hitActor,
                damageData = damageData,
                staggerRating = 0,
                knockbackStrength = 0,
                impactRotation = collisionEvent.impactRotation,
                impactPosition = collisionEvent.impactPosition,
                tick = tick,
            };

            HitUtility.ProcessHit(ref hit, projectile.Context);
        }

        public static void HandleImpact(Projectile projectile,
            ProjectileDefinition definition,
            ref FProjectileData data,
            ref FPhysicsHitData impactHit,
            int tick)
        {
            projectile.SetImpactData(ref data, impactHit.ImpactPoint, tick);

            projectile.Position = impactHit.ProjectilePosition;
            
            SpawnImpactEffect(projectile, ref data, ref impactHit, tick);

            if (projectile is FixedUpdateProjectile fixedUpdateProjectile)
            {
                definition.SpawnImpactProjectiles(ref data, ref impactHit, fixedUpdateProjectile);
                //definition.TriggerImpactActions(ref data, ref impactHit, fixedUpdateProjectile);
            }
        }

        public static void SpawnImpactEffect(Projectile projectile,
            ref FProjectileData data,
            ref FPhysicsHitData impactHit,
            int tick)
        {
            Vector2 spawnPosition = impactHit.ImpactPoint;

            /*
            projectile.OwningPool.Context.ImpactManager.SpawnImpact
            (
                projectile.Instigator.NetActor,
                projectile.Definition.ImpactDefinition,
                spawnPosition,
                projectile.Definition.Height,
                projectile.FixedUpdateRotation,
                impactHit.HitTarget,
                new FDamageData(),
                tick);
            */

        }
    }
}