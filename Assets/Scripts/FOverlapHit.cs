
// Used for overlap collision in projectile utility

namespace LichLord
{
    using UnityEngine;

    public struct FOverlapHit
    {
        public GameObject GameObject;
        public Collider Collider;
        public Vector3 HitPoint;
        public Vector3 Normal;
        public float Distance;
    }
}
