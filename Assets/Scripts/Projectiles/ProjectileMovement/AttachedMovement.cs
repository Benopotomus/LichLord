
namespace LichLord.Projectiles
{
    using UnityEngine;

    [CreateAssetMenu(menuName = "LichLord/Projectiles/AttachedMovement")]
    public class AttachedMovement : ProjectileMovement
    {
        [SerializeField] private EMuzzle _attachment;
        public EMuzzle Attachment => Attachment;

        public override void OnRender(RenderProjectile projectile,
            ref FProjectileData toData,
            ref FProjectileData fromData,
            float bufferAlpha,
            float deltaTime,
            float renderTimeSinceFired,
            int tick)
        {

            projectile.Position = MuzzleUtility.GetMuzzlePosition(projectile.Instigator.NetActor, _attachment);
            Debug.Log(projectile.Position);
            projectile.Velocity = Vector2.zero;
            projectile.Rotation = GetRotation(projectile.Definition, toData.TargetPosition.Position, toData.Position.Position, projectile.Velocity);
        }

        public override void OnFixedUpdate(FixedUpdateProjectile projectile, ref FProjectileData data, int tick, float simulationTime, float deltaTime)
        {
            float simTimeSinceFired = simulationTime - (data.FireTick * projectile.Runner.DeltaTime);

            Vector3 newPosition = MuzzleUtility.GetMuzzlePosition(projectile.Instigator.NetActor, _attachment);

            Vector3 newVelocity = Vector3.zero;

            Quaternion oldRotation = projectile.Rotation;
            Quaternion newRotation = GetRotation(
                projectile.Definition,
                data.TargetPosition.Position,
                newPosition,
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
