
namespace LichLord.Projectiles
{
    using System.Collections.Generic;
    using UnityEngine;
    using Fusion;

    public class FixedUpdateProjectile
    {
        
        public int Index { get; set; }
        public ProjectilePool OwningPool { get; set; }
        public SceneContext Context { get; set; }
        public PlayerRef StateAuthority { get; set; }
        public NetworkRunner Runner => OwningPool.Runner;

        public ProjectileDefinition Definition { get; private set; }
        public IHitInstigator Instigator { get; set; }
        public INetActor Target { get; private set; }
        public float Timestamp { get; set; }
        public int FireTick { get; set; }

        public FProjectilePayload Payload = new FProjectilePayload();
        public FProjectilePayload Payload_SpawnedProjectile = new FProjectilePayload();

        public Vector3 FixedUpdatePosition { get; set; }
        public Quaternion FixedUpdateRotation { get; set; }
        public Vector3 FixedUpdateVelocity { get; set; }

        public float Lifetime { get; set; }
        public bool IsDataSet { get; set;}

        //FProjectileCollisionEvent _collisionEvent = new FProjectileCollisionEvent();

        // Collisions
        private List<IHitTarget> AffectedActors = new List<IHitTarget>();

        // FIXED UPDATE

        public void ActivateFixedUpdate(ref FProjectileData data, 
            ref FProjectilePayload payload,
            ref FProjectilePayload payload_spawnedProjectile)
        {
            SetData(ref data);
            Payload.Copy(ref payload);
            Payload_SpawnedProjectile.Copy(ref payload_spawnedProjectile);
            AffectedActors.Clear();
        }

        public void SetData(ref FProjectileData data)
        {
            Definition = Global.Tables.ProjectileTable.TryGetDefinition(data.DefinitionID);
            Timestamp = data.FireTick * Runner.DeltaTime;
            FireTick = data.FireTick;
            Instigator = data.InstigatorID.GetHitInstigator(Runner);
            FixedUpdatePosition = data.Position;
            IsDataSet = true;
        }

        public void DeactivateFixedUpdate(ref FProjectileData data)
        {
            data.IsFinished = true;
            data.IsHoming = false;
            data.FireTick = Runner.Tick;
        }

        public void OnLifetimeExpired(ref FProjectileData data)
        {
            DeactivateFixedUpdate(ref data);
        }

        public void OnFixedUpdate(ref FProjectileData data, int tick, float simulationTime, float deltaTime)
        {
            if (data.IsFinished)
                return;

            if (!IsDataSet) 
                SetData(ref data);

            if (Definition == null)
                return;

            if (Runner.SimulationTime >= (data.FireTick * deltaTime) + Lifetime)
            {
                OnLifetimeExpired(ref data);
            }

            ProjectileMovement projectileMovement = Definition.ProjectileMovement;
            if (projectileMovement == null)
                return;

            projectileMovement.OnFixedUpdate(this, ref data, tick, simulationTime, deltaTime);
        }

        public void UpdateAffectedActors(ref FProjectileData data, List<FPhysicsHitData> hitDatas, int tick)
        {
            if (Definition.AffectsEachCast)
            {
                AffectedActors.Clear();
            }

            // Process new hits and add them if not already affected
            for (int i = 0; i < hitDatas.Count; i++)
            {
                IHitTarget hitTarget = hitDatas[i].HitTarget;
                if (hitTarget == null || AffectedActors.Contains(hitTarget))
                    continue;

                AffectedActors.Add(hitTarget);
                FPhysicsHitData hitData = hitDatas[i];
                CollideWithHitData(ref data, ref hitData, tick);
            }

            // If it can hit more than once, remove actors no longer in the hit list
            if (!Definition.OnlyAffectsOnce)
            {
                // Use a loop instead of a HashSet to reduce memory allocations
                for (int j = AffectedActors.Count - 1; j >= 0; j--)
                {
                    IHitTarget affectedActor = AffectedActors[j];
                    if (affectedActor == null)
                    {
                        AffectedActors.RemoveAt(j);
                        continue;
                    }

                    // Directly check if the affected actor exists in hitDatas instead of using a HashSet
                    bool isStillHit = false;
                    for (int i = 0; i < hitDatas.Count; i++)
                    {
                        if (hitDatas[i].HitTarget == affectedActor)
                        {
                            isStillHit = true;
                            break;
                        }
                    }

                    if (!isStillHit)
                    {
                        AffectedActors.RemoveAt(j);
                    }
                }
            }
        }

        private void CollideWithHitData(ref FProjectileData data, ref FPhysicsHitData hitData, int tick)
        {
            /*
            Vector2 sourcePosition = hitData.ProjectilePosition;
            Vector2 targetTestPosition = hitData.HitObject.transform.position.ToVector2();
            IHitTarget currentActor = hitData.HitTarget;

            switch (Definition.LineOfSightRequirement)
            {
                case eLineOfSightRequirement.LOS_To_Instigator_Feet:

                    sourcePosition = Instigator.NetActor.GlobalPosition();

                    if (!HallowHeartHelpers.IsPointVisible(sourcePosition, targetTestPosition,
                        Definition.LineOfSightLayer, GetIgnoreGameObjects(currentActor.NetActor)))
                        return;
                    break;

                case eLineOfSightRequirement.LOS_To_Instigator_SkillComponent:

                    sourcePosition = NetActorUtility.GetActorCenter(Instigator.NetActor);

                    if (!HallowHeartHelpers.IsPointVisible(sourcePosition, targetTestPosition,
                        Definition.LineOfSightLayer, GetIgnoreGameObjects(currentActor.NetActor))) // if the target actor is not visible, ignore
                        return;
                    break;

                case eLineOfSightRequirement.LOS_To_Projectile_Center:
                    if (!HallowHeartHelpers.IsPointVisible(sourcePosition, targetTestPosition,
                        Definition.LineOfSightLayer, GetIgnoreGameObjects(currentActor.NetActor))) // if the target actor is not visible, ignore
                        return;
                    break;
                
            }
            HandleActorCollision(currentActor, ref data, sourcePosition, ref hitData, tick);
            */
        }

        
        public void HandleActorCollision(IHitTarget collidedActor, 
            ref FProjectileData data, 
            Vector2 sourcePosition,
            ref FPhysicsHitData hitdata,
            int tick)
        {
            /*
            _collisionEvent.projectile = this;
            _collisionEvent.hitTarget = collidedActor;
            _collisionEvent.collideTick = OwningPool.Runner.Tick;
            _collisionEvent.impactPosition = sourcePosition;
            _collisionEvent.impactRotationRadians = GetImpactRotationRadiansFromActor(ref hitdata);

            Definition.ImpactResponse.HandleCollisionHitActor(this, ref data, ref _collisionEvent, tick);
        */
        }

        public void HandleImpact(IHitTarget collidedActor,
            ref FProjectileData data,
            ref FPhysicsHitData hitdata,
            int tick)
        {
            /*
            _collisionEvent.projectile = this;
            _collisionEvent.hitTarget = collidedActor;
            _collisionEvent.collideTick = OwningPool.Runner.Tick;
            _collisionEvent.impactPosition = hitdata.ProjectilePosition;
            _collisionEvent.impactRotationRadians = GetImpactRotationRadiansFromActor(ref hitdata);

            if (!data.InstigatorEffectApplied)
            {
                Definition.ImpactResponse.SpawnGameplayEffectsForInstigator(this, ref data, ref _collisionEvent, tick);
                data.InstigatorEffectApplied = true;
            }
            */
        }

        public Quaternion GetImpactRotationRadiansFromActor(ref FPhysicsHitData hitData)
        {
            return GetImpactRotation(hitData.HitObject.transform.position);
        }

        public Quaternion GetImpactRotation(Vector3 impactPosition)
        {
            /*
            //Debug.Log("Hit Object Position: " + impactPosition + ", FixedUpdatePosition: " + FixedUpdatePosition);
            switch (Definition.EffectSource)
            {
                case eEffectSource.PROJECTILE_FORWARD:
                    return FixedUpdateRotation;

                case eEffectSource.PROJECTILE_CENTER:
                    Vector2 directionVector = (impactPosition - FixedUpdatePosition).normalized;
                    return Mathf.Atan2(directionVector.y, directionVector.x);     
                    
                case eEffectSource.OWNER_CENTER:
                    Vector2 directionBetweenActors = 
                        (impactPosition - NetActorUtility.GetActorCenter(Instigator.NetActor)).normalized;
                    return Mathf.Atan2(directionBetweenActors.y, directionBetweenActors.x);

                case eEffectSource.OWNER_FORWARD:
                    Vector2 aimVector = Instigator.NetActor.GetAimVector();
                    return  Mathf.Atan2(aimVector.y, aimVector.x);
                default:
                    return 0f;
            }
            */

            return Quaternion.identity;
        }

        private List<GameObject> GetIgnoreGameObjects(INetActor currentActor)
        {
            List<GameObject> ignoredGameObjects = new List<GameObject>();
            /*
            if (Instigator.NetActor.ActorNode != null) // instigator can be null before this goes through
            {
                HitboxComponent ignoredHitBox = NetActorUtility.GetActorHitbox(Instigator.NetActor);

                if (ignoredHitBox != null)
                    ignoredGameObjects.Add(ignoredHitBox.gameObject);
            }

            if (currentActor.ActorNode != null)
            {
                ignoredGameObjects.Add(currentActor.ActorNode);

                HitboxComponent ignoredHitBox = currentActor.ActorNode.GetComponent<HitboxComponent>();

                if (ignoredHitBox != null)
                    ignoredGameObjects.Add(ignoredHitBox.gameObject);
            }
            */
            return ignoredGameObjects;
        }

        public void SpawnDeactivationProjectiles(ref FProjectileData data, ref FPhysicsHitData impactHit)
        {
            /*
            FProjectileFireEvent fireEvent = new FProjectileFireEvent();
            for (int i = 0; i < Definition.DeactivationSpawnedProjectiles.Count; i++)
            {
                ProjectileManager.CreateProjectileFireEvent(
                    ref fireEvent,
                    Definition.DeactivationSpawnedProjectiles[i],
                    Instigator,
                    new FNetObjectID(),
                    impactHit.ProjectilePosition,
                    impactHit.ProjectilePosition + (data.TargetPosition - data.Position).ToVector2().normalized,
                    OwningPool.Runner.Tick,
                    ref Payload_SpawnedProjectile,
                    ref Payload_SpawnedProjectile,
                    0,
                    false,
                    Vector2.zero,
                    0
                    );

                OwningPool.SpawnProjectile(fireEvent);
            }
            */
        }
        
    }
}
