
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
            projectile.Position = data.Position.Position;
            projectile.Velocity = GetInitialVelocity(projectile.Definition, data.TargetPosition.Position, data.Position.Position);
            projectile.Rotation = GetRotation(projectile.Definition,
                ref data,
                data.TargetPosition.Position,
                data.Position.Position, 
                projectile.Velocity, 
                projectile.Rotation);
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
            return toData.Position.Position;
        }

        // FIXED UPDATE
        public virtual void ActivateFixedUpdate(FixedUpdateProjectile projectile, 
            ref FProjectileData data)
        {
            projectile.Position = data.Position.Position;
            projectile.Velocity = GetInitialVelocity(projectile.Definition, data.TargetPosition.Position, data.Position.Position);
            projectile.Rotation = GetRotation(projectile.Definition,
                ref data, 
                data.TargetPosition.Position, 
                data.Position.Position,
                projectile.Velocity,
                projectile.Rotation);
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
            return toData.Position.Position;
        }

        public virtual Vector3 GetInitialVelocity(ProjectileDefinition definition,
            Vector3 targetPositon, 
            Vector3 spawnPosition)
        {
            return (targetPositon - spawnPosition).normalized;
        }

        public virtual Quaternion GetRotation(ProjectileDefinition definition, 
            ref FProjectileData data,
            Vector3 targetPosition, 
            Vector3 currentPosition, 
            Vector3 velocity,
            Quaternion oldRotation)
        {
            if (data.HasImpacted)
                return oldRotation;

            // I need a way here that if the the rotation on this is going to be quaternion identy, use the old rrotation

            switch (definition.RotationType)
            {
                case ERotationType.FaceVelocity:
                    if (velocity.sqrMagnitude < 0.0001f)
                        return oldRotation;

                    return Quaternion.LookRotation(velocity.normalized);
                case ERotationType.FaceTarget:
                    Vector3 direction = (targetPosition - currentPosition).normalized;
                    if (direction.sqrMagnitude < 0.0001f)
                        return oldRotation;

                    return Quaternion.LookRotation(direction);

                default:
                    return Quaternion.identity;
            }
        }
    }
}

