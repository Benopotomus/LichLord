using UnityEngine;

namespace LichLord
{
    public struct FPhysicsHitData
    {
        public bool IsAssigned;
        public GameObject HitObject;
        public IHitTarget HitTarget;
        public Vector3 ProjectilePosition;
        public Vector3 ImpactVelocity;
        public Vector3 ImpactPoint;
        public Vector3 HitNormal;
    }
}