
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

        /*
        public virtual void SpawnGameplayEffectsForInstigator(Projectile projectile,
            ref FProjectileData data,
            ref FProjectileCollisionEvent collisionEvent,
            int tick)
        {
            INetActor instigator = projectile.Instigator.NetActor;
            INetActor target = collisionEvent.hitTarget != null ? collisionEvent.hitTarget.NetActor : null;

            Vector2 outPosition = collisionEvent.impactPosition;

            if (projectile.Payload.instigatorGameplayEffects == null)
                return;

            foreach (var gpeDefinition in projectile.Payload.instigatorGameplayEffects)
            {
                GameplayEffectManager.CreateGameplayEffectEvent(ref projectile.GameplayEffectEvent,
                    gpeDefinition,
                    instigator,
                    instigator,
                    target,
                    outPosition,
                    tick);

                projectile.Context.GameplayEffectManager.SpawnGameplayEffect(ref projectile.GameplayEffectEvent);
            }
        }

        public virtual void SpawnGameplayEffectsForTarget(Projectile projectile,
            ref FProjectileData data,
            ref FProjectileCollisionEvent collisionEvent,
            int tick)
        {
            if (collisionEvent.hitTarget == null)
                return;

            Vector2 direction = new Vector2(Mathf.Cos(collisionEvent.impactRotationRadians),
                Mathf.Sin(collisionEvent.impactRotationRadians));
            Vector2 outPosition = collisionEvent.impactPosition + (direction * 10f);

            if (projectile.Payload.targetGameplayEffects == null)
                return;

            foreach (var gpeDefinition in projectile.Payload.targetGameplayEffects)
            {
                FGameplayEffectEvent effectEvent = new FGameplayEffectEvent();
                GameplayEffectManager.CreateGameplayEffectEvent(ref effectEvent,
                    gpeDefinition,
                    projectile.Instigator.NetActor,
                    collisionEvent.hitTarget.NetActor,
                    null,
                    outPosition,
                    tick);

                projectile.Context.GameplayEffectManager.SpawnGameplayEffect(ref effectEvent);
            }
        }

        public bool HandleReflect(Projectile projectile,
            ref FProjectileData data,
            ref FProjectileCollisionEvent collisionEvent,
            int tick)
        {
            if (!projectile.Definition.Reflectable)
                return false;

            // Check if Reflect
            INetActor hitActor = collisionEvent.hitTarget.NetActor;

            EffectSystemComponent effectSystem = hitActor.GetEffectSystemComponent();
            if (effectSystem == null)
                return false;

            if (effectSystem.ReflectProjectilesAngle == 0)
                return false;

            float incomingAngle = Vector2.Angle(-projectile.FixedUpdateVelocity, hitActor.GetAimVector());
            if (incomingAngle < effectSystem.ReflectProjectilesAngle)
            {
                Vector2 reflection = Vector2.Reflect(projectile.FixedUpdateVelocity, hitActor.GetAimVector());

                data.Position = collisionEvent.impactPosition.ToShortVector();
                data.TargetPosition = (collisionEvent.impactPosition + (reflection * 10f)).ToShortVector();
                data.InstigatorID = hitActor.NetObjectID;
                data.FireTick = tick;
                data.IsReflected = true;
                projectile.SetData(ref data);

                return true;
            }


            return false;
        }

        */
        public static void HandleImpact(Projectile projectile,
            ref FProjectileData data,
            ref FPhysicsHitData impactHit,
            int tick)
        {

            data.HasImpacted = true;
            data.TargetPosition.CopyPosition(impactHit.ImpactPoint);

            //Debug.Log(projectile.Index +  " Impacted on fixed Update " + impactHit.ImpactPoint);

            projectile.ImpactTick = tick;

            projectile.Position = impactHit.ProjectilePosition;
            
            SpawnImpactEffect(projectile, ref data, ref impactHit, tick);

            if (projectile is FixedUpdateProjectile fixedUpdateProjectile)
            {
                fixedUpdateProjectile.SpawnDeactivationProjectiles(ref data, ref impactHit);
                //fixedUpdateProjectile.DeactivateFixedUpdate(ref data);
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