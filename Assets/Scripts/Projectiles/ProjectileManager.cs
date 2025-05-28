
namespace LichLord.Projectiles
{
    using UnityEngine;

    [DefaultExecutionOrder(0)]
    public class ProjectileManager : ContextBehaviour
    {
        /*
        [SerializeField]
        private ServerProjectilePool _serverProjectilePool;

        public FixedUpdateProjectile SpawnProjectile(FProjectileFireEvent fireEvent)
        {
            ProjectilePool projectilePool = GetProjectilePoolForInstigator(fireEvent.instigator);

            return projectilePool.SpawnProjectile(fireEvent);
        }

        public ProjectilePool GetProjectilePoolForInstigator(IHitInstigator instigator)
        {
            return GetProjectilePoolForActor(instigator.NetActor);
        }

        public ProjectilePool GetProjectilePoolForActor(INetActor actor)
        {
            if (actor is HeroEntity hero)
            {
                PlayerEntity playerEntity = Context.NetworkGame.GetPlayerEntity(hero.Object.InputAuthority);

                if (playerEntity == null)
                {
                    Debug.LogError("Spawning Projectile without a player pool");
                }

                return playerEntity.ProjectilePool;
            }

            return _serverProjectilePool;
        }

        public static void CreateProjectileFireEvent(
            ref FProjectileFireEvent fireEvent,
            ProjectileDefinition projectileDefinition,
            IHitInstigator instigator,
            FNetObjectID targetID,
            Vector2 spawnPosition,
            Vector2 targetPosition,
            int fireTick,
            ref FProjectilePayload payload,
            ref FProjectilePayload payload_spawnedProjectile,
            float angleOffset,
            bool invertAngle,
            Vector2 overrideSize,
            float overrideLifetime)
        {
            fireEvent.projectileDefinition = projectileDefinition;

            fireEvent.instigator = instigator;
            fireEvent.targetId = targetID;
            fireEvent.fireTick = fireTick;

            fireEvent.spawnPosition = spawnPosition.ToShortVector();
            fireEvent.targetPosition = targetPosition.ToShortVector();

            fireEvent.payload.Copy(ref payload);
            fireEvent.payload_spawnedProjectile.Copy(ref payload_spawnedProjectile);

            // calculate the angle between the spawn position and target position
            Vector2 direction = targetPosition - spawnPosition;
            float angleTo = Mathf.Atan2(direction.y, direction.x);  // Angle in radians

            // calculate the distance between the spawn position and target position
            float dist = Vector2.Distance(spawnPosition, targetPosition);

            // convert angleOffset from degrees to radians
            float angleOffsetRad = Mathf.Deg2Rad * angleOffset;

            // calculate the new angle based on the original angle and angle offset
            float newAngle = 0;
            if (invertAngle)
            {
                newAngle = angleTo - angleOffsetRad;  // Subtract the offset when inverted
            }
            else
            {
                newAngle = angleTo + angleOffsetRad;  // Add the offset normally
            }

            // Normalize the angle to keep it within the 0 to 2π range (0 to 360 degrees in radians)
            newAngle = Mathf.Repeat(newAngle, Mathf.PI * 2);

            // calculate the new target position based on the new angle and distance
            Vector2 newTargetPosition = spawnPosition + new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * dist;
            fireEvent.targetPosition = newTargetPosition.ToShortVector();

            fireEvent.overrideSize = overrideSize;
            fireEvent.overrideLifetime = overrideLifetime;
        }

        public static void CreateReflectedProjectileFireEvent(ref FProjectileFireEvent fireEvent, 
            FProjectileCollisionEvent collisionEvent, 
            FixedUpdateProjectile reflectedProjectile) // REFLECTED SHOT
        {
            fireEvent.projectileDefinition = reflectedProjectile.Definition;
            fireEvent.fireTick = collisionEvent.collideTick;
            fireEvent.spawnPosition = collisionEvent.impactPosition.ToShortVector();

            fireEvent.payload.Copy(ref reflectedProjectile.Payload);
            fireEvent.payload_spawnedProjectile.Copy(ref reflectedProjectile.Payload_SpawnedProjectile);
        }
        */
    }
}