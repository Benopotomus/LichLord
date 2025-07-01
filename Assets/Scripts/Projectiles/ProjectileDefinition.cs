using DWD.AnimationCurveAsset;
using DWD.Utility.Loading;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Projectiles
{
    [CreateAssetMenu(menuName = "LichLord/Projectiles/ProjectileDefinition")]
    public class ProjectileDefinition : TableObject
    {
        //Definitions
        [SerializeField]
        protected int _lifetimeTicks;
        public int LifetimeTicks => _lifetimeTicks;

        [SerializeField]
        protected List<ProjectileDefinition> _lifetimeExpiredSpawnedProjectiles = new List<ProjectileDefinition>(); // projectiles spawned on deactivation (hit or not);
        public List<ProjectileDefinition> LifetimeExpiredSpawnedProjectiles => _lifetimeExpiredSpawnedProjectiles;

        public void SpawnLifetimeExpiredProjectiles(ref FProjectileData data, FixedUpdateProjectile projectile)
        {
            for (int i = 0; i < LifetimeExpiredSpawnedProjectiles.Count; i++)
            {
                projectile.SpawnProjectile(ref data, LifetimeExpiredSpawnedProjectiles[i], projectile.Position);
            }
        }

        // Movement
        [SerializeField]
        protected ProjectileMovement _projectileMovement;
        public ProjectileMovement ProjectileMovement => _projectileMovement;

        [SerializeField]
        protected float _speed; // linear speed
        public float Speed => _speed;

        // The layers that will cause the projectile to impact (stop and destroy, or bounce etc.)
        [SerializeField]
        protected LayerMask _impactCollisionLayer;
        public LayerMask ImpactCollisionLayer => _impactCollisionLayer;

        // The layers that will cause the projectile to damage and apply effects
        [SerializeField]
        protected LayerMask _overlapCollisionLayer;
        public LayerMask OverlapCollisionLayer => _overlapCollisionLayer;

        [SerializeField]// if this range is exceeded. do something
        protected EShapeType _shape;
        public EShapeType Shape => _shape;

        // Line of Sight
        [SerializeField]
        protected ELineOfSightRequirement _lineOfSightRequirement; // if the hit occurs, ensure that we have LOS from the instigator to the target or fail.
        public ELineOfSightRequirement LineOfSightRequirement => _lineOfSightRequirement;

        // Line of Sight
        [SerializeField]
        protected ERotationType _rotationType; // if the hit occurs, ensure that we have LOS from the instigator to the target or fail.
        public ERotationType RotationType => _rotationType;

        [SerializeField]
        protected Vector3 _extents;
        public Vector3 Extents => _extents;

        [SerializeField]
        protected bool _onlyAffectsOnce;
        public bool OnlyAffectsOnce => _onlyAffectsOnce;

        [SerializeField]
        protected bool _affectsEachCast;
        public bool AffectsEachCast => _affectsEachCast;

        [SerializeField]
        protected EPhysicsSweep _physicsSweep;
        public EPhysicsSweep PhysicsSweep => _physicsSweep;

        [SerializeField]
        protected Vector2 _collisionCheckTrim; // the time after spawn where it wont check. Second value is from the lifetime end.
        public Vector2 CollisionCheckTrim => _collisionCheckTrim;

        [SerializeField]
        protected int _collisionCheckRate; // ticks it will check at
        public int CollisionCheckRate => _collisionCheckRate;

        [SerializeField]
        protected int _collisionCheckNumber = 4; // the amount of colliders this can affect each collision check
        public int CollisionCheckNumber => _collisionCheckNumber;

        [SerializeField]
        protected bool _onlyAffectImpactTarget; // the amount of colliders this can affect each collision check
        public bool OnlyAffectImpactTarget => _onlyAffectImpactTarget;

        [SerializeField]
        protected LayerMask _lineOfSightLayer;
        public LayerMask LineOfSightLayer => _lineOfSightLayer;

        [SerializeField]
        protected AnimationCurveAsset _scaleOverLifetime;
        public AnimationCurveAsset ScaleOverLifetime => _scaleOverLifetime;

        //Visuals
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _visualsPrefab;
        public BundleObject VisualsPrefab => _visualsPrefab;

        //[SerializeField]
        //protected ImpactDefinition _impactDefinition;
        //public ImpactDefinition ImpactDefinition => _impactDefinition;

        [SerializeField]
        protected bool _impactSticksToHitActor; // Offset the height from its actual location off the ground
        public bool ImpactSticksToHitActor => _impactSticksToHitActor;

        [SerializeField]// if this range is exceeded. do something
        protected bool _homesAtApex;
        public bool HomesAtApex => _homesAtApex;

        [Header("Damage")]
        [SerializeField]
        protected int _damage = 10;
        public int Damage => _damage;

        [SerializeField]
        protected EDamageType _damageType = EDamageType.None;
        public EDamageType DamageType => _damageType;

        [Header("Impact")]
        [SerializeField]
        protected float _postImpactTicks = 16;
        public float PostImpactTicks => _postImpactTicks;

        [SerializeField]
        protected List<ProjectileDefinition> _impactSpawnedProjectiles = new List<ProjectileDefinition>(); // projectiles spawned on deactivation (hit or not);
        public List<ProjectileDefinition> ImpactSpawnedProjectiles => _impactSpawnedProjectiles;

        public void SpawnImpactProjectiles(ref FProjectileData data, ref FPhysicsHitData impactHit, FixedUpdateProjectile projectile)
        {
            for (int i = 0; i < ImpactSpawnedProjectiles.Count; i++)
            {
                projectile.SpawnProjectile(ref data, ImpactSpawnedProjectiles[i], impactHit.ImpactPoint);
            }
        }

        [Header("Proximity Detonation")]
        [SerializeField]// if this range is exceeded. do something
        protected float _proximityDetonationRange;
        public float ProximityDetonationRange => _proximityDetonationRange;

        [SerializeField]// if this range is exceeded. do something
        protected int _proximityDetonationTicks;
        public int ProximityDetonationTicks => _proximityDetonationTicks;

        [SerializeField]
        protected List<ProjectileDefinition> _proximityDetonationProjectile = new List<ProjectileDefinition>(); // projectiles spawned on deactivation (hit or not);
        public List<ProjectileDefinition> ProximityDetonationProjectile => _proximityDetonationProjectile;

        public void UpdateProximityFuse(ref FProjectileData data, int tick, FixedUpdateProjectile projectile)
        {
            if (ProximityDetonationRange > 0)
            {
                if (data.IsProximityFuseActive)
                {
                    if (tick > projectile.FuseDetonationTick)
                    {
                        if (!data.HasImpacted)
                        {
                            SpawnProximityDetonationProjectiles(ref data, projectile);
                            projectile.SetImpactData(ref data, projectile.Position, tick);
                        }
                    }
                }
                else
                {
                    ProjectilePhysicsUtility.CheckProximityFuse(ref data, projectile, this, tick);
                }
            }
        }

        public void SpawnProximityDetonationProjectiles(ref FProjectileData data, FixedUpdateProjectile projectile)
        {
            for (int i = 0; i < ProximityDetonationProjectile.Count; i++)
            {
                projectile.SpawnProjectile(ref data, ProximityDetonationProjectile[i], projectile.Position);
            }
        }

        [Header("Timed Fuse Detonation")]

        [SerializeField]
        protected bool _hasTimedFuse;
        public bool HasTimedFuse => _hasTimedFuse;

        // The minimum ticks and fuse ticks 
        [SerializeField]
        protected Vector2Int _timedFuseTickRange;
        public Vector2Int TimedFuseTickRange => _timedFuseTickRange;

        // The distance for min ticks and max ticks
        [SerializeField]
        protected Vector2 _timedFuseDistanceRange;
        public Vector2 TimedFuseDistanceRange => _timedFuseDistanceRange;

        [SerializeField]
        protected List<ProjectileDefinition> _timedFuseDetonationProjectiles = new List<ProjectileDefinition>(); // projectiles spawned on deactivation (hit or not);
        public List<ProjectileDefinition> TimedFuseDetonationProjectiles => _timedFuseDetonationProjectiles;

        public void SetTimedFuseTick(ref FProjectileData data, ref FProjectileFireEvent fireEvent)
        {
            float distance = Vector3.Distance(fireEvent.spawnPosition, fireEvent.targetPosition);

            // Clamp the distance to the allowed fuse distance range
            float clampedDistance = Mathf.Clamp(distance, TimedFuseDistanceRange.x, TimedFuseDistanceRange.y);

            // Calculate normalized distance percent (0 to 1)
            float distancePercent = Mathf.InverseLerp(
                TimedFuseDistanceRange.x,
                TimedFuseDistanceRange.y,
                clampedDistance
            );

            // Map percent to ticks
            int fuseTicks = Mathf.RoundToInt(Mathf.Lerp(
                TimedFuseTickRange.x,
                TimedFuseTickRange.y,
                distancePercent
            ));

            data.FuseData.FuseCompleteTick = fireEvent.fireTick + fuseTicks;
        }

        public void UpdateTimedFuse(ref FProjectileData data, int tick, FixedUpdateProjectile projectile)
        {
            if (!HasTimedFuse)
                return;

            if(data.HasImpacted)
                return;

            if (tick > data.FuseData.FuseCompleteTick)
            { 
                projectile.SetImpactData(ref data, projectile.Position, tick);
                SpawnTimedFuseDetonationProjectiles(ref data, projectile);
            }
        }

        public void SpawnTimedFuseDetonationProjectiles(ref FProjectileData data, FixedUpdateProjectile projectile)
        {
            for (int i = 0; i < TimedFuseDetonationProjectiles.Count; i++)
            {
                projectile.SpawnProjectile(ref data, TimedFuseDetonationProjectiles[i], projectile.Position);
            }
        }

        [Header("NPC Aiming")]

        [SerializeField]
        protected bool _forcesRemoteAiming;
        public bool ForcesRemoteAiming => _forcesRemoteAiming;

    }

    public enum EPhysicsSweep
    { 
        None,
        Overlap,
        Cast,
    }
}

