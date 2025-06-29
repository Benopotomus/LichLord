namespace LichLord.Projectiles
{
    using UnityEngine;

    public struct FProjectileFireEvent
    {
        public ProjectileDefinition projectileDefinition;
        public IHitInstigator instigator;
        public FNetObjectID targetId;
        public int fireTick;

        public Vector3 spawnPosition;
        public Vector3 targetPosition;

        public FProjectilePayload payload;
        public FProjectilePayload payload_spawnedProjectile;
    }
}