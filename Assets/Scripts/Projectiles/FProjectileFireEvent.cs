namespace LichLord.Projectiles
{
    using Fusion;

    public struct FProjectileFireEvent
    {
        public ProjectileDefinition projectileDefinition;
        public IHitInstigator instigator;
        public FNetObjectID targetId;
        public int fireTick;

        public Vector3Compressed spawnPosition;
        public Vector3Compressed targetPosition;

        public FProjectilePayload payload;
        public FProjectilePayload payload_spawnedProjectile;
    }
}