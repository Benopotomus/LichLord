

namespace LichLord
{
    using Fusion;
    using UnityEngine;

    public enum EHitAction : byte
    {
        None,
        Damage,
        Heal,
    }

    public struct FHitUtilityData
    {
        public IHitInstigator Instigator;
        public IHitTarget Target;
        //public FDamageData DamageData;
        public int StaggerRating;
        public float KnockbackStrength;
        public bool IsFatal;
        public Vector2 ImpactPosition;
        public float ImpactRadians;
        public bool IsPredictiveHit;
        public PlayerRef PlayerRef;
        public uint PredictionKey;
        public int Tick;
        public bool IsBlockable;
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
        bool IsActive { get; }
        INetActor NetActor { get; }
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
            hit.Target.ProcessHit(ref hit);
            hit.Instigator.HitPerformed(ref hit);
            return hit;
        }
    }
}
