
namespace LichLord.Projectiles
{
    using UnityEngine;

    [CreateAssetMenu(menuName = "LichLord/Projectiles/LinearMovement")]
    public class LinearMovement: ProjectileMovement
    {
        // RENDER

        public override void OnRender(RenderProjectile projectile,
            ref FProjectileData toData,
            ref FProjectileData fromData,
            float bufferAlpha,
            float deltaTime,
            float renderTimeSinceFired,
            int tick)
        {
            Vector3 lastPosition = projectile.Position;
            Vector3 newRenderTargetPosition = GetLinearMovePosition(projectile.Definition, ref toData, renderTimeSinceFired);
            float interpolationProgress = 0f;

            // Do not start interpolation until projectile should actually move
            projectile.InterpolationTime += Time.deltaTime;
            interpolationProgress = Mathf.Clamp01(projectile.InterpolationTime / projectile.InterpolationDuration);
            
            var lerpPosition = Vector3.Lerp(projectile.Position, newRenderTargetPosition, interpolationProgress);

            projectile.Position = lerpPosition;
            projectile.Velocity = projectile.Position - lastPosition;
            projectile.Rotation = GetRotation(projectile.Definition, toData.TargetPosition.Position, toData.Position.Position, projectile.Velocity);
        }

        private Vector3 GetLinearMovePosition(ProjectileDefinition definition, 
            ref FProjectileData toData, 
            float timeSinceFired)
        {
            
            if (timeSinceFired <= 0f)
                return toData.Position.Position;

            Vector3 velocity = Vector3CompressedExtensions.SubtractAndNormalize(toData.TargetPosition.Position, toData.Position.Position) * definition.Speed;
            return toData.Position.Position + (velocity * timeSinceFired);
        }

        public override Vector3 GetInitialVelocity(ProjectileDefinition definition, 
            Vector3 targetPositon, 
            Vector3 spawnPosition)
        {
            return Vector3.zero;
        }

        // FIXED UPDATE

        public override void OnFixedUpdate(FixedUpdateProjectile projectile, 
            ref FProjectileData data, 
            int tick, 
            float simulationTime, 
            float deltaTime)
        {
            if (data.HasImpacted)
                return;

            ProjectileDefinition definition = projectile.Definition;

            float lastTimeSinceFired = ((tick - data.FireTick) - 1) * deltaTime;
            float newTimeSinceFired = (tick - data.FireTick) * deltaTime;
            Vector3 oldPosition = GetLinearMovePosition(definition,  ref data, lastTimeSinceFired);
            Vector3 newPosition = GetLinearMovePosition(definition,  ref data, newTimeSinceFired);

            Vector3 newVelocity = newPosition - oldPosition;

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
                oldPosition, 
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
