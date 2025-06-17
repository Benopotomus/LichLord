using LichLord.Projectiles;
using System;

namespace LichLord
{
    [Serializable]
    public struct FManeuverProjectile
    {
        public int SpawnTick;
        public ProjectileDefinition Definition;
        public bool SpawnsOnAnimationNotify;
        public EMuzzle Muzzle;
        public FDamagePotential Damage;
    }
}
