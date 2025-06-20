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
        protected float _lifetime;
        public float Lifetime => _lifetime;

        //Definitions
        [SerializeField]
        protected float _postImpactLifetime = 0.2f;
        public float PostImpactLifetime => _postImpactLifetime;

        [SerializeField]
        protected List<ProjectileDefinition> _deactivationSpawnedProjectiles = new List<ProjectileDefinition>(); // projectiles spawned on deactivation (hit or not);
        public List<ProjectileDefinition> DeactivationSpawnedProjectiles => _deactivationSpawnedProjectiles;

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
    }

    public enum EPhysicsSweep
    { 
        None,
        Overlap,
        Cast,
    }
}

