
namespace LichLord.Projectiles
{
    using System.Collections.Generic;
    using UnityEngine;
    using Fusion;

    public class FixedUpdateProjectile : Projectile
    {
        public PlayerRef StateAuthority { get; set; }

        public float Lifetime { get; set; }
        public bool IsDataSet { get; set;}

        // FIXED UPDATE

        public void ActivateFixedUpdate(ref FProjectileData data, 
            ref FProjectilePayload payload,
            ref FProjectilePayload payload_spawnedProjectile)
        {
            SetData(ref data);
            Payload.Copy(ref payload);
            Payload_SpawnedProjectile.Copy(ref payload_spawnedProjectile);
            AffectedActors.Clear();
            
            if (Definition != null)
            {
                Definition.ProjectileMovement.ActivateFixedUpdate(this, ref data);
            }

            data.IsActive = true;
        }

        public void SetData(ref FProjectileData data)
        {
            Instigator = data.InstigatorID.GetHitInstigator(Runner);
            Definition = Global.Tables.ProjectileTable.TryGetDefinition(data.DefinitionID);
            Timestamp = data.FireTick * Runner.DeltaTime;
            FireTick = data.FireTick;
            Position = data.Position.Position;
            TargetPosition = data.TargetPosition.Position;
            IsDataSet = true;
        }

        public void DeactivateFixedUpdate(ref FProjectileData data)
        {
            Instigator = null;
            Definition = null;
            data.IsFinished = true;
            data.IsHoming = false;
            data.HasImpacted = false;
            data.FireTick = Runner.Tick;
            data.IsActive = false;
        }

        public void OnLifetimeExpired(ref FProjectileData data)
        {
            DeactivateFixedUpdate(ref data);
        }

        public void OnFixedUpdate(ref FProjectileData data, int tick, float simulationTime, float deltaTime)
        {
            if (!data.IsActive)
                return;

            if (data.IsFinished)
                return;

            if (!IsDataSet) 
                SetData(ref data);

            if (Definition == null)
                return;

            if (simulationTime >= (data.FireTick * deltaTime) + Definition.Lifetime)
            {
                OnLifetimeExpired(ref data);
                return;
            }

            if (data.HasImpacted)
            {
                if (simulationTime >= +(ImpactTick * deltaTime) + Definition.PostImpactLifetime)
                {
                    DeactivateFixedUpdate(ref data);
                }

                return;
            }

            ProjectileMovement projectileMovement = Definition.ProjectileMovement;
            if (projectileMovement == null)
                return;

            projectileMovement.OnFixedUpdate(this, ref data, tick, simulationTime, deltaTime);
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
            
            FProjectileFireEvent fireEvent = new FProjectileFireEvent();
            for (int i = 0; i < Definition.DeactivationSpawnedProjectiles.Count; i++)
            {
                ProjectileManager.CreateProjectileFireEvent(
                    ref fireEvent,
                    Definition.DeactivationSpawnedProjectiles[i],
                    Instigator,
                    new FNetObjectID(),
                    impactHit.ProjectilePosition,
                    impactHit.ProjectilePosition + Vector3CompressedExtensions.SubtractAndNormalize(data.TargetPosition.Position, data.Position.Position), 
                    OwningPool.Runner.Tick,
                    ref Payload_SpawnedProjectile,
                    ref Payload_SpawnedProjectile);


                OwningPool.SpawnProjectile(fireEvent);
            }
            
        }
        
    }
}
