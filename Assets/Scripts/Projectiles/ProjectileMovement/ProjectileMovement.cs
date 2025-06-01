
namespace LichLord.Projectiles
{
    using System.Collections.Generic;
    using UnityEngine;
    using Fusion;

    public class ProjectileMovement : ScriptableObject
    {
        // RENDER
        public virtual void ActivateRender(RenderProjectile projectile, 
            ref FProjectileData data)
        {
            projectile.RenderPosition = data.Position;
        }

        public virtual void OnRender(RenderProjectile projectile,
            ref FProjectileData toData,
            ref FProjectileData fromData,
            float bufferAlpha,
            float deltaTime,
            float renderTimeSinceFired)
        { 
        }

        protected virtual Vector3 GetRenderPosition(RenderProjectile projectile, 
            ref FProjectileData toData, 
            ref FProjectileData fromData, 
            float alpha)
        {
            return toData.Position;
        }

        // FIXED UPDATE
        public virtual void ActivateFixedUpdate(FixedUpdateProjectile projectile, 
            ref FProjectileData data)
        { 
        }

        public virtual void OnFixedUpdate(FixedUpdateProjectile projectile, 
            ref FProjectileData data, 
            int tick, 
            float simulationTime, 
            float deltaTime)
        {
        }

        protected virtual void CheckAndHandleCollision(FixedUpdateProjectile projectile,
            ref FProjectileData data,
            int tick,
            float simulationTime,
            Vector3 oldPosition,
            Vector3 newPosition,
            Quaternion oldRotation,
            Quaternion newRotation)
        {
            
            float simTimeSinceFired = simulationTime - (data.FireTick * projectile.Runner.DeltaTime);
            /*
            if (ProjectileUtility.IsCollidingActive(projectile.Definition, simTimeSinceFired))
            {
                int collisionCheckRate = projectile.Definition.CollisionCheckRate;

                // Check every tick if rate is 0, otherwise follow the tick interval
                if (collisionCheckRate == 0 || (tick % collisionCheckRate == 0))
                {
                    PerformCollisionChecks(projectile, ref data, tick, simulationTime, 
                        oldPosition, newPosition, 
                        oldRotation, newRotation);
                }
            }
            */
        }

        protected virtual void PerformCollisionChecks(FixedUpdateProjectile projectile,
            ref FProjectileData data,
            int tick,
            float simulationTime,
            Vector3 oldPosition,
            Vector3 newPosition,
            Quaternion oldRotation,
            Quaternion newRotation)
        {
            /*
            ProjectileDefinition definition = projectile.Definition;

            FPhysicsHitData impactHit = new FPhysicsHitData();
            List<FPhysicsHitData> hitDatas = new List<FPhysicsHitData>();
            
            data.IsReflected = false;

            int sweepCount = Mathf.Max(definition.CollisionSweepsPerTick, 1);
            for (int i = 1; i <= sweepCount; i++)
            {
                float sweepLerp = i / (float)sweepCount;
                Vector2 sweepLocation = Vector2.Lerp(oldPosition, newPosition, sweepLerp);
                float sweepRotation = Mathf.Lerp(oldRotation, newRotation, sweepLerp);

                // Perform the shape collision check
                ProjectileUtility.CheckShapeCollisions(projectile,
                    ref data,
                    tick,
                    simulationTime,
                    sweepLocation,
                    sweepRotation,
                    ref hitDatas,
                    ref impactHit);

                if (definition.OnlyAffectImpactTarget && impactHit.IsAssigned)
                {
                    hitDatas = new List<FPhysicsHitData> { impactHit };
                    projectile.UpdateAffectedActors(ref data, hitDatas, tick);
                }
                else
                {
                    projectile.UpdateAffectedActors(ref data, hitDatas, tick);
                }

                // If there's an impact, handle it
                if (impactHit.IsAssigned)
                {
                    definition.ImpactResponse.HandleImpact(projectile, ref data, ref impactHit, tick);
                    break;
                }
            }
            */
        }

        // BOTH

        protected virtual Vector3 GetMovePosition(ProjectileDefinition definition, 
            NetworkRunner runner, 
            ref FProjectileData toData, 
            float currentTick,
            float deltaTime)
        {
            return toData.Position;
        }

        public virtual Vector3 GetInitialVelocity(ProjectileDefinition definition,
            Vector3 targetPositon, 
            Vector3 spawnPosition)
        {
            return Vector3.zero;
        }

        public virtual Quaternion GetRotation(ProjectileDefinition definition, 
            Vector3 targetPosition, 
            Vector3 spawnPosition, 
            Vector3 velocity)
        {
            /*
            switch (definition.RotationType)
            {
                case eProjectileRotationType.FACE_TARGET:
                    Vector2 direction = (targetPosition - spawnPosition).normalized;
                    return Mathf.Atan2(direction.y, direction.x);

                case eProjectileRotationType.NO_ROTATION:
                    return 0f;

                case eProjectileRotationType.FACE_VELOCITY:
                    Vector2 normalizeVelocity = velocity.normalized;
                    return Mathf.Atan2(normalizeVelocity.y, normalizeVelocity.x);

                default:
                    return 0f;
            }
            */

            return Quaternion.identity;
        }
    }
}

