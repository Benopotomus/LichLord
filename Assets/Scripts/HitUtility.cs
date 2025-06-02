

namespace LichLord
{
    using UnityEngine;

    public enum EHitAction : byte
    {
        None,
        Damage,
        Heal,
    }

    public struct FHitUtilityData
    {
        public IHitInstigator instigator;
        public IHitTarget target;
        public FDamageData damageData;
        public int staggerRating;
        public float knockbackStrength;
        public bool isFatal;
        public Vector3 impactPosition;
        public Quaternion impactRotation;
        public int tick;
    }

    public enum EHitType
    {
        None,
        Projectile,
        Explosion,
        Suicide,
    }

    public interface IHitTarget
    {
        void ProcessHit(ref FHitUtilityData hit);
        void OnHitTaken(ref FHitUtilityData hit);
    }

    public interface IHitInstigator
    {
        INetActor NetActor { get; }
        void HitPerformed(ref FHitUtilityData hit);
    }

    /// <summary>
    /// A utility that encapsulates common approach of handling hits.
    /// </summary>
    public static class HitUtility
    {
        // PUBLIC METHODS

        public static FHitUtilityData ProcessHit(ref FHitUtilityData hit,
            SceneContext context)
        {
            hit.target.ProcessHit(ref hit);
            hit.instigator.HitPerformed(ref hit);
            return hit;
        }
    }
}
