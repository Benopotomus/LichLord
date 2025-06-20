
namespace LichLord.Projectiles
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "LichLord/Projectiles/StationaryMovement")]
    public class StationaryMovement : ProjectileMovement
    {
        public override void OnRender(RenderProjectile projectile,
            ref FProjectileData toData,
            ref FProjectileData fromData,
            float bufferAlpha,
            float deltaTime,
            float renderTimeSinceFired,
            int tick)
        {
            Vector2 lastPosition = projectile.Position;
            projectile.Position = toData.Position.Position;
            projectile.Velocity = Vector2.zero;
            projectile.Rotation = GetRotation(projectile.Definition, projectile.Position, projectile.Position, projectile.Velocity);
        }

        public override void OnFixedUpdate(FixedUpdateProjectile projectile, ref FProjectileData data, int tick, float simulationTime, float deltaTime)
        {
            float simTimeSinceFired = simulationTime - (data.FireTick * projectile.Runner.DeltaTime);

            Vector3 newPosition = data.Position.Position;

            Vector3 newVelocity = Vector3.zero;

            Quaternion oldRotation = projectile.Rotation;
            Quaternion newRotation = GetRotation(
                projectile.Definition,
                data.TargetPosition.Position,
                data.Position.Position,
                projectile.Velocity);

            ProjectilePhysicsUtility.CheckAndHandleCollision(projectile,
                ref data,
                tick,
                simulationTime,
                deltaTime,
                newPosition,
                newPosition,
                oldRotation,
                newRotation);

            if (data.IsFinished)
                return;

            projectile.Position = newPosition;
            projectile.Velocity = newVelocity;
            projectile.Rotation = newRotation;
        }      
    }
}
