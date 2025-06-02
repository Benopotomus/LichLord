
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
            projectile.Position = data.Position;
            projectile.Velocity = GetInitialVelocity(projectile.Definition, data.TargetPosition, data.Position);
            projectile.Rotation = GetRotation(projectile.Definition, data.TargetPosition, data.Position, projectile.Velocity);
        }

        public virtual void OnRender(RenderProjectile projectile,
            ref FProjectileData toData,
            ref FProjectileData fromData,
            float bufferAlpha,
            float deltaTime,
            float renderTimeSinceFired,
            int tick)
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
            return (targetPositon - spawnPosition).normalized;
        }

        public virtual Quaternion GetRotation(ProjectileDefinition definition, 
            Vector3 targetPosition, 
            Vector3 currentPosition, 
            Vector3 velocity)
        {
            switch (definition.RotationType)
            {
                case ERotationType.FaceVelocity:
                    if (velocity.sqrMagnitude < 0.0001f)
                        return Quaternion.identity;

                    return Quaternion.LookRotation(velocity.normalized);
                case ERotationType.FaceTarget:
                    Vector3 direction = (targetPosition - currentPosition).normalized;
                    if (direction.sqrMagnitude < 0.0001f)
                        return Quaternion.identity;

                    return Quaternion.LookRotation(velocity.normalized);

                default:
                    return Quaternion.identity;
            }
        }
    }
}

