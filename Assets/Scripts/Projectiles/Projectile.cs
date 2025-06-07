using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Projectiles
{
    public class Projectile
    {
        public ProjectilePool OwningPool { get; set; }
        public SceneContext Context { get; set; }
        public NetworkRunner Runner => OwningPool.Runner;

        public ProjectileDefinition Definition { get; protected set; }
        public IHitInstigator Instigator { get; set; }
        public INetActor Target { get; private set; }
        public float Timestamp { get; set; }
        public int FireTick { get; set; }

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Velocity { get; set; }

        protected List<IHitTarget> AffectedActors = new List<IHitTarget>();

        FProjectileCollisionEvent _collisionEvent = new FProjectileCollisionEvent();

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
            
            Vector2 impactPosition = hitData.ProjectilePosition;
            Vector2 targetTestPosition = hitData.HitObject.transform.position.ToVector2();
            IHitTarget hitTarget = hitData.HitTarget;
            /*
       switch (Definition.LineOfSightRequirement)
       {

           case ELineOfSightRequirement.LOS_To_Instigator_Feet:

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
    */
            _collisionEvent.projectile = this;
            _collisionEvent.hitTarget = hitTarget;
            _collisionEvent.collideTick = OwningPool.Runner.Tick;
            _collisionEvent.impactPosition = impactPosition;
            _collisionEvent.impactRotation = GetImpactRotation(impactPosition);

            ProjectileImpactUtility.HandleCollisionHitActor(this, ref data, ref _collisionEvent, tick);
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
    }
}
