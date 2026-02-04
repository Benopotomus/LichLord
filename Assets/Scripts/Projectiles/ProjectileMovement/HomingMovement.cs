
namespace LichLord.Projectiles
{
    using UnityEngine;
    using UnityEngine.InputSystem.Controls;

    [CreateAssetMenu(menuName = "LichLord/Projectiles/HomingMovement")]
    public class HomingMovement: ProjectileMovement
    {

        [SerializeField]
        private int _ticksToHome;
        public int TicksToHome => _ticksToHome;

        [SerializeField]
        private float _rotationSpeed;
        public float RotationSpeed => _rotationSpeed;

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

            projectile.Position = Vector3.Lerp(projectile.Position, toData.Position.Position, bufferAlpha);
            projectile.Velocity = projectile.Position - lastPosition;
            projectile.Rotation = GetRotation(projectile.Definition,
                ref toData,
                toData.TargetPosition.Position,
                toData.Position.Position,
                projectile.Velocity,
                projectile.Rotation);
        }

        private Vector3 GetLinearMovePosition(ProjectileDefinition definition, 
            ref FProjectileData data, 
            float timeSinceFired,
            float deltaTime)
        {
            if (data.HasImpacted)
            {
                return data.Position.Position;
            }

            if (timeSinceFired <= 0f)
                return data.Position.Position;

            Vector3 direction = (data.TargetPosition.Position - data.Position.Position).normalized;

            Vector3 velocity = direction * definition.Speed;
            Vector3 newPosition = data.Position.Position + (velocity * deltaTime);

            return newPosition;
        }

        private Vector3 GetHomingMovePosition(FixedUpdateProjectile projectile, ProjectileDefinition definition,
            ref FProjectileData data,
            float timeSinceFired,
            float deltaTime)
        {
            if (data.HasImpacted)
            {
                return data.Position.Position;
            }

            if (timeSinceFired <= 0f)
                return data.Position.Position;

            IHitTarget target = projectile.Target;

            Vector3 direction =  (target.ChunkTrackable.Position - data.Position.Position).normalized;

            if (RotationSpeed > 0f)
            {
                // Gradual turn limited by rotation speed
                float maxRadiansDelta = RotationSpeed * Mathf.Deg2Rad * deltaTime;
                direction = Vector3.RotateTowards(projectile.Rotation * Vector3.forward, direction, maxRadiansDelta, 0).normalized;
            }

            Vector3 velocity = direction * definition.Speed;
            Vector3 newPosition = data.Position.Position + (velocity * deltaTime);

            return newPosition;
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

            Vector3 oldPosition = data.Position.Position;
            Vector3 newPosition = oldPosition;
            Vector3 newVelocity = Vector3.zero;
            IHitTarget target = projectile.Target;

            if (projectile.TicksSinceFired >= TicksToHome)
            {
                data.IsHoming = true;
            }

            if (data.IsHoming && target != null)
            {
                newPosition = GetHomingMovePosition(projectile, definition, ref data, newTimeSinceFired, deltaTime);
            }
            else
            {
                newPosition = GetLinearMovePosition(definition, ref data, newTimeSinceFired, deltaTime);
            }

            newVelocity = newPosition - oldPosition;

            Quaternion oldRotation = projectile.Rotation;
            Quaternion newRotation = GetRotation(
                projectile.Definition,
                ref data,
                data.TargetPosition.Position,
                data.Position.Position,
                projectile.Velocity,
                projectile.Rotation);

            ProjectilePhysicsUtility.CheckAndHandleCollision(projectile, 
                ref data, 
                tick, 
                simulationTime,
                deltaTime, 
                oldPosition, 
                newPosition,
                oldRotation,
                newRotation);

            projectile.Position = newPosition;
            projectile.Velocity = newVelocity;
            projectile.Rotation = newRotation;

            data.Position.CopyPosition(newPosition);
        }
    }
}
