using UnityEngine;

namespace LichLord
{
    public struct FQueryShape
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public EShapeType shapeType;
        public Vector3 shapeExtents;
    }

    public enum EShapeType
    {
        None,
        Sphere,
        Raycast,
        Capsule,
    }

    public struct QueryTransform
    {
        public Vector3 position;
        public Vector3 eulerAngles;
        public Vector3 scale;
    }

    public enum eEffectSource
    {
        None,
        ProjectileForward,
        ProjectileCenter,
        OwnerForward,
        OwnerCenter,
        OwnerFeet,
    }
}