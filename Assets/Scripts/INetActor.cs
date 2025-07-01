using LichLord.Projectiles;

namespace LichLord
{
    public interface INetActor
    {
        public FNetObjectID NetObjectID
        {
            get;
        }

        void ProjectileSpawnedCallback(Projectile projectile, ProjectileDefinition definition, ref FProjectileData data);
    }
}