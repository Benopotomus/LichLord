using UnityEngine;

namespace LichLord
{
    public struct FPhysicsHitData
    {
        public bool IsAssigned;
        public GameObject HitObject;
        public IHitTarget HitTarget;
        public Vector2 ProjectilePosition;
        public Vector2 ImpactVelocity;
        public Vector2 ImpactPoint;
        public Vector2 HitNormal;
    }
}