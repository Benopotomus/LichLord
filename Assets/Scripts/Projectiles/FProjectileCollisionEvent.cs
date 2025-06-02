using UnityEngine;
using Fusion;

/// <summary>
/// This impact data and is all calculated on the server and not networked
/// </summary>
namespace LichLord.Projectiles
{
    public struct FProjectileCollisionEvent
    {
        public Projectile projectile;
        public IHitTarget hitTarget;

        public int collideTick;
        public Vector3 impactPosition;
        public Quaternion impactRotation;
    }
}

