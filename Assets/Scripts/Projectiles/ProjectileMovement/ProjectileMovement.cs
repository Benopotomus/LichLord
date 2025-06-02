
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

