
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
            float renderTimeSinceFired)
        {
            Vector3 lastPosition = projectile.RenderPosition;
            Vector3 newRenderTargetPosition = GetLinearMovePosition(projectile.Definition, ref toData, renderTimeSinceFired);
            float interpolationProgress = 0f;

            // Do not start interpolation until projectile should actually move
            projectile.InterpolationTime += Time.deltaTime;
            interpolationProgress = Mathf.Clamp01(projectile.InterpolationTime / projectile.InterpolationDuration);
            
            var lerpPosition = Vector3.Lerp(projectile.RenderPosition, newRenderTargetPosition, interpolationProgress);

            projectile.RenderPosition = lerpPosition;
            projectile.RenderVelocity = projectile.RenderPosition - lastPosition;
            projectile.RenderRotation = GetRotation(projectile.Definition, toData.TargetPosition, toData.Position, projectile.RenderVelocity);
        }

        private Vector3 GetLinearMovePosition(ProjectileDefinition definition, 
            ref FProjectileData toData, 
            float timeSinceFired)
        {
            
            if (timeSinceFired <= 0f)
                return toData.Position;

            Vector3 velocity = Vector3CompressedExtensions.SubtractAndNormalize(toData.TargetPosition, toData.Position) * definition.Speed;
            return toData.Position + (velocity * timeSinceFired);
        }

        public override Vector3 GetInitialVelocity(ProjectileDefinition definition, 
            Vector3 targetPositon, 
            Vector3 spawnPosition)
        {
            return Vector3.zero;
            // return (targetPositon - spawnPosition).normalized * definition.Speed;
        }

        // FIXED UPDATE

        public override void OnFixedUpdate(FixedUpdateProjectile projectile, 
            ref FProjectileData data, 
            int tick, 
            float simulationTime, 
            float deltaTime)
        {
            ProjectileDefinition definition = projectile.Definition;

            float lastTimeSinceFired = ((tick - data.FireTick) - 1) * deltaTime;
            float newTimeSinceFired = (tick - data.FireTick) * deltaTime;
            Vector2 oldPosition = GetLinearMovePosition(definition,  ref data, lastTimeSinceFired);
            Vector2 newPosition = GetLinearMovePosition(definition,  ref data, newTimeSinceFired);

            Vector2 newVelocity = newPosition - oldPosition;

            Quaternion oldRotation = projectile.FixedUpdateRotation;
            Quaternion newRotation = GetRotation(
                projectile.Definition,
                data.TargetPosition,
                data.Position,
                projectile.FixedUpdateVelocity);

            //CheckAndHandleCollision(projectile, ref data, tick, simulationTime, oldPosition, newPosition, oldRotation, newRotation);

            projectile.FixedUpdatePosition = newPosition;
            projectile.FixedUpdateVelocity = newVelocity;
            projectile.FixedUpdateRotation = newRotation;
        }
    }
}
