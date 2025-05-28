namespace LichLord.Projectiles
{
    using Fusion;
    using System.Collections.Generic;
    using UnityEngine;
    //using DWD.AnimationCurveAsset;

    public static class ProjectileUtility
    {
        /*
        public static void GetValidOverlapHits(FixedUpdateProjectile projectile,
            ref FProjectileData data,
            ref FQueryShape queryShape,
            ref List<FPhysicsHitData> hitDatas,
            ref FPhysicsHitData impactHit)
        {
            List<LagCompensatedHit> hitResults = GetOverlapHits(projectile, ref queryShape);

            LayerMask impactLayer = projectile.Definition.ImpactCollisionLayer;
            LayerMask overlapLayer = projectile.Definition.OverlapCollisionLayer;

            Vector2 startPosition = queryShape.position;

            foreach (LagCompensatedHit hitResult in hitResults)
            {
                GameObject gameObjectHit = hitResult.GameObject;

                IHitTarget hitTarget = null;

                if (gameObjectHit.tag == "HitboxBody" || gameObjectHit.tag == "HitboxFeet")
                {
                    HitboxOwner hitboxOwnerComp = gameObjectHit.GetComponent<HitboxOwner>();
                    if (hitboxOwnerComp == null)
                        continue;

                    hitTarget = hitboxOwnerComp.HitTarget;

                    if (!IsImpactObjectValid(projectile, gameObjectHit, hitTarget.NetActor))
                        continue;
                }
                
                // Check for impact layer
                if ((impactLayer.value & (1 << gameObjectHit.layer)) != 0)
                {
                    if (!impactHit.IsAssigned)
                    {
                        impactHit.IsAssigned = true;
                        impactHit.HitObject = gameObjectHit;
                        impactHit.HitTarget = hitTarget;
                        impactHit.ProjectilePosition = queryShape.position;
                        impactHit.ImpactVelocity = projectile.FixedUpdateVelocity;
                        impactHit.ImpactPoint = hitResult.Point;
                        impactHit.HitNormal = hitResult.Normal;

                        hitDatas.Add(impactHit);
                        continue;
                    }
                }

                // Check for to for overlap layer
                if ((overlapLayer.value & (1 << gameObjectHit.layer)) != 0)
                {
                    FPhysicsHitData hitData = new FPhysicsHitData();
                    hitData.IsAssigned = true;
                    hitData.HitObject = gameObjectHit;
                    hitData.HitTarget = hitTarget;
                    hitData.ProjectilePosition = queryShape.position;
                    hitData.ImpactVelocity = projectile.FixedUpdateVelocity;
                    hitData.ImpactPoint = hitResult.Point;
                    impactHit.HitNormal = hitResult.Normal;

                    hitDatas.Add(hitData);
                }
            }
        }

        public static List<LagCompensatedHit> GetOverlapHits(FixedUpdateProjectile projectile, 
            ref FQueryShape queryShape)
        {
            ProjectileDefinition definition = projectile.Definition;
            NetworkRunner runner = projectile.Runner;
            PlayerRef inputAuthority = projectile.OwningPool.Object.InputAuthority;

            List<LagCompensatedHit> hitResults = new List<LagCompensatedHit>();

            int hitCount = 0;
            Vector2 position = queryShape.position;
            Quaternion rotation = Quaternion.Euler(0, 0, queryShape.eulerAngles.z);

            switch (queryShape.shapeType)
            {
                case eShapeType.RAYCAST:
                    hitCount = runner.LagCompensation.RaycastAll(
                        origin: position,
                        direction: rotation * Vector3.right,
                        length: queryShape.shapeExtents.x,
                        player: inputAuthority,
                        hits: hitResults,
                        layerMask: definition.OverlapCollisionLayer,
                        clearHits: true,
                        HitOptions.IncludeBox2D | HitOptions.SubtickAccuracy);

                    break;
                case eShapeType.CIRCLE:
                    hitCount = runner.LagCompensation.OverlapSphere(
                        origin: position,
                        radius: queryShape.shapeExtents.x,
                        player: inputAuthority,
                        hits: hitResults,
                        layerMask: definition.OverlapCollisionLayer,
                        HitOptions.IncludeBox2D | HitOptions.SubtickAccuracy);

                    break;
                case eShapeType.BOX:
                    hitCount = runner.LagCompensation.OverlapBox(
                        center: position,
                        extents: queryShape.shapeExtents,
                        orientation: rotation,
                        player: inputAuthority,
                        hits: hitResults,
                        layerMask: definition.OverlapCollisionLayer,
                        HitOptions.IncludeBox2D | HitOptions.SubtickAccuracy);
                    break;
                case eShapeType.OVAL:

                    float scale = queryShape.shapeExtents.x;
                    // CenterCircle
                    hitCount = runner.LagCompensation.OverlapSphere(
                        origin: position,
                        radius: scale * 0.376f,
                        player: inputAuthority,
                        hits: hitResults,
                        layerMask: definition.OverlapCollisionLayer,
                        HitOptions.IncludeBox2D | HitOptions.SubtickAccuracy);

                    // Left Circle
                    List<LagCompensatedHit> hitResultsLeft = new List<LagCompensatedHit>();
                    Vector3 leftPosition = new Vector3(position.x - (0.168f * scale), position.y, 0);
                    hitCount += runner.LagCompensation.OverlapSphere(
                        origin: leftPosition,
                        radius: scale * 0.333f,
                        player: inputAuthority,
                        hits: hitResultsLeft,
                        layerMask: definition.OverlapCollisionLayer,
                        HitOptions.IncludeBox2D | HitOptions.SubtickAccuracy);
                    hitResults.AddRange(hitResultsLeft);

                    // Right Circle
                    List<LagCompensatedHit> hitResultsRight = new List<LagCompensatedHit>();
                    Vector3 rightPosition = new Vector3(position.x + (0.168f * scale), position.y, 0);
                    hitCount += runner.LagCompensation.OverlapSphere(
                        origin: rightPosition,
                        radius: scale * 0.333f,
                        player: inputAuthority,
                        hits: hitResultsRight,
                        layerMask: definition.OverlapCollisionLayer,
                        HitOptions.IncludeBox2D | HitOptions.SubtickAccuracy);
                    hitResults.AddRange(hitResultsRight);

                    break;
            }

            if (hitCount == 0)
                return hitResults;

            // Filter hitResults to only keep the first hit per GameObject.
            List<LagCompensatedHit> filteredHits = new List<LagCompensatedHit>(hitResults.Count);
            HashSet<GameObject> seenGameObjects = new HashSet<GameObject>();

            foreach (LagCompensatedHit hit in hitResults)
            {
                GameObject gameObjectHit = hit.GameObject;
                if (gameObjectHit != null && !seenGameObjects.Contains(gameObjectHit))
                {
                    filteredHits.Add(hit);
                    seenGameObjects.Add(gameObjectHit);
                }
            }

            return hitResults;
        }

        public static float GetScaleAtTime(ProjectileDefinition definition, float timeSinceFire)
        {
            if (definition == null)
                return 1f;

            AnimationCurveAsset animationCurveDefinition = definition.ScaleOverLifetime;
            if (animationCurveDefinition == null)
                return 1f;

            // Ensure that lifetime is greater than zero to avoid division by zero
            if (definition.Lifetime <= 0f)
                return 1f;

            // Calculate the percentage of the lifetime that has expired
            float percentLifetimeExpired = timeSinceFire / definition.Lifetime;

            // Clamp the value between 0 and 1 to ensure valid percentage
            percentLifetimeExpired = Mathf.Clamp01(percentLifetimeExpired);

            return animationCurveDefinition.Curve.Evaluate(percentLifetimeExpired);

        }

        public static bool IsImpactObjectValid(FixedUpdateProjectile projectile, GameObject hitObject, INetActor hitActor)
        {
            // Early exit for untagged or irrelevant objects
            if (projectile == null)
                return false;

            if (projectile.Instigator == null)
                return false;

            if (hitActor == null)
                return true;

            eTeamAttitude attitude = TeamUtility.GetAttitudeForNetActors(projectile.Instigator.NetActor, hitActor);
            if (attitude == eTeamAttitude.Friendly)
                return false; // Skip friendly objects

                
            EffectSystemComponent effectSystemComponent = hitActor.GetEffectSystemComponent();

            if (effectSystemComponent != null)
            {
                if (effectSystemComponent.IsHitImmune)
                    return false;
            }
                
            return true;
        }

        public static bool IsCollidingActive(ProjectileDefinition definition, float simTimeSinceFired)
        {
            if (definition.Shape == eShapeType.NONE)
                return false;

            // See if it can collide yet.
            if (definition.CollisionCheckTrim.x != 0.0f && definition.CollisionCheckTrim.x > simTimeSinceFired)
                return false;

            if (definition.CollisionCheckTrim.y != 0.0f && simTimeSinceFired > definition.CollisionCheckTrim.y)
                return false;

            return true;
        }

        public static void CheckShapeCollisions(FixedUpdateProjectile projectile, 
            ref FProjectileData data, 
            int tick, 
            float simTimeSinceFired, 
            Vector2 position, 
            float rotation, 
            ref List<FPhysicsHitData> hitDatas, 
            ref FPhysicsHitData impactHit)
        {
            ProjectileDefinition definition = projectile.Definition;

            FQueryShape _currentQueryShape = new FQueryShape();

            _currentQueryShape.position = position;
            _currentQueryShape.eulerAngles.z = definition.UsePositionOnly ? 0.0f : Mathf.Rad2Deg * rotation;
            _currentQueryShape.shapeType = definition.Shape;
            _currentQueryShape.shapeExtents = definition.Extents * GetScaleAtTime(definition, simTimeSinceFired);

            GetValidOverlapHits(projectile, ref data, ref _currentQueryShape, ref hitDatas, ref impactHit);
        }
        */
    }
}
