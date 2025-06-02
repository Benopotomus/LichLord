
namespace LichLord.Projectiles
{
    using UnityEngine;

    public class ProjectileManager : ContextBehaviour
    {
        [SerializeField]
        private ServerProjectilePool _serverProjectilePool;

        public FixedUpdateProjectile SpawnProjectile(FProjectileFireEvent fireEvent)
        {
            ProjectilePool projectilePool = GetProjectilePoolForInstigator(fireEvent.instigator);

            return projectilePool.SpawnProjectile(fireEvent);
        }

        public ProjectilePool GetProjectilePoolForInstigator(IHitInstigator instigator)
        {
            if (instigator is PlayerCreature playerCreature)
            {
                if (playerCreature.HasStateAuthority)
                {
                    return playerCreature.ProjectilePool;
                }
            }

            return _serverProjectilePool;
        }

        public static void CreateProjectileFireEvent(
            ref FProjectileFireEvent fireEvent,
            ProjectileDefinition projectileDefinition,
            IHitInstigator instigator,
            FNetObjectID targetID,
            Vector3 spawnPosition,
            Vector3 targetPosition,
            int fireTick,
            ref FProjectilePayload payload,
            ref FProjectilePayload payload_spawnedProjectile)
        {
            fireEvent.projectileDefinition = projectileDefinition;

            fireEvent.instigator = instigator;
            fireEvent.targetId = targetID;
            fireEvent.fireTick = fireTick;

            fireEvent.spawnPosition = spawnPosition;
            fireEvent.targetPosition = targetPosition;

            fireEvent.payload.Copy(ref payload);
            fireEvent.payload_spawnedProjectile.Copy(ref payload_spawnedProjectile);
        }

    }
}