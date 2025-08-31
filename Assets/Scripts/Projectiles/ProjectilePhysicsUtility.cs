namespace LichLord.Projectiles
{
    using DWD.AnimationCurveAsset;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Pool;

    public struct FOverlapHit
    {
        public GameObject GameObject;
        public Collider Collider;
        public Vector3 HitPoint;
        public Vector3 Normal;
        public float Distance;
    }

    public static class ProjectilePhysicsUtility
    {
        // Static pools for arrays and HashSet to avoid allocations
        private static readonly RaycastHit[] rayHitsPool = new RaycastHit[maxCollisionCheckNumber]; // Define maxCollisionCheckNumber (e.g., 32)
        private static readonly RaycastHit[] sphereHitsPool = new RaycastHit[maxCollisionCheckNumber];
        private static readonly Collider[] collidersPool = new Collider[maxCollisionCheckNumber];
        private static readonly HashSet<GameObject> seenGameObjectsPool = new HashSet<GameObject>();

        // Adjust this value based on the maximum expected CollisionCheckNumber
        private const int maxCollisionCheckNumber = 64;

        public static void CheckAndHandleCollision(Projectile projectile,
            ref FProjectileData data,
            int tick,
            float simulationTime,
            float networkDeltaTime,
            Vector3 oldPosition,
            Vector3 newPosition,
            Quaternion oldRotation,
            Quaternion newRotation)
        {
            float simTimeSinceFired = simulationTime - (data.FireTick * projectile.Runner.DeltaTime);

            if (IsCollidingActive(projectile.Definition, simTimeSinceFired))
            {
                int collisionCheckRate = projectile.Definition.CollisionCheckRate;

                // Check every tick if rate is 0, otherwise follow the tick interval
                if (collisionCheckRate == 0 || (tick % collisionCheckRate == 0))
                {
                    PerformCollisionChecks(projectile, 
                        ref data, 
                        tick, 
                        simTimeSinceFired, 
                        networkDeltaTime,
                        oldPosition, newPosition,
                        oldRotation, newRotation);
                }
            }
        }

        private static void PerformCollisionChecks(Projectile projectile,
            ref FProjectileData data,
            int tick,
            float simTimeSinceFired,
            float networkDeltaTime,
            Vector3 oldPosition,
            Vector3 newPosition,
            Quaternion oldRotation,
            Quaternion newRotation)
        {
            ProjectileDefinition definition = projectile.Definition;

            FPhysicsHitData impactHit = new FPhysicsHitData();
            List<FPhysicsHitData> hitDatas = ListPool<FPhysicsHitData>.Get();

            // Perform the shape collision check
            CheckShapeCollisions(projectile,
                ref data,
                tick,
                simTimeSinceFired,
                networkDeltaTime,
                oldPosition,
                newPosition,
                newRotation,
                ref hitDatas,
                ref impactHit);

            if (definition.OnlyAffectImpactTarget && impactHit.IsAssigned)
            {
                List<FPhysicsHitData> singleHitData = ListPool<FPhysicsHitData>.Get();
                singleHitData.Add(impactHit);
                projectile.UpdateAffectedActors(ref data, singleHitData, tick);
                ListPool<FPhysicsHitData>.Release(singleHitData);
            }
            else
            {
                projectile.UpdateAffectedActors(ref data, hitDatas, tick);
            }

            // If there's an impact, handle it
            if (impactHit.IsAssigned)
            {
                ProjectileImpactUtility.HandleImpact(projectile, 
                    definition,
                    ref data,
                    ref impactHit, 
                    tick);
            }

            // Release the hitDatas list
            ListPool<FPhysicsHitData>.Release(hitDatas);
        }

        public static void CheckShapeCollisions(Projectile projectile,
            ref FProjectileData data,
            int tick,
            float simTimeSinceFired,
            float networkDeltaTime,
            Vector3 oldPosition,
            Vector3 position,
            Quaternion rotation,
            ref List<FPhysicsHitData> hitDatas,
            ref FPhysicsHitData impactHit)
        {
            ProjectileDefinition definition = projectile.Definition;

            FQueryShape _currentQueryShape = new FQueryShape
            {
                lastPostion = oldPosition,
                position = position,
                rotation = rotation,
                shapeType = definition.Shape,
                shapeExtents = definition.Extents * GetScaleAtTime(definition, simTimeSinceFired, networkDeltaTime)
            };

            GetValidHits(projectile, ref data, ref _currentQueryShape, ref hitDatas, ref impactHit);
        }

        public static void GetValidHits(Projectile projectile,
            ref FProjectileData data,
            ref FQueryShape queryShape,
            ref List<FPhysicsHitData> hitDatas,
            ref FPhysicsHitData impactHit)
        {
            List<FOverlapHit> hitResults = ListPool<FOverlapHit>.Get();

            switch (projectile.Definition.PhysicsSweep)
            {
                case EPhysicsSweep.Overlap:
                    hitResults = GetOverlapHits(projectile, ref queryShape);
                    break;
                case EPhysicsSweep.Cast:
                    hitResults = GetCastHits(projectile, ref queryShape);
                    break;
            }

            LayerMask impactLayer = projectile.Definition.ImpactCollisionLayer;
            LayerMask overlapLayer = projectile.Definition.OverlapCollisionLayer;

            Vector2 startPosition = queryShape.position;

            foreach (var hitResult in hitResults)
            {
                GameObject gameObjectHit = hitResult.GameObject;

                IHitTarget hitTarget = null;

                if (gameObjectHit.tag == "Hurtbox")
                {
                    HurtboxOwner hitboxOwnerComp = gameObjectHit.GetComponent<HurtboxOwner>();
                    if (hitboxOwnerComp == null)
                        continue;

                    hitTarget = hitboxOwnerComp.HitTarget;

                    if (!IsImpactObjectValid(projectile, gameObjectHit, hitTarget))
                        continue;
                }

                // Check for impact layer
                if ((impactLayer.value & (1 << gameObjectHit.layer)) != 0)
                {
                    if (!impactHit.IsAssigned)
                    {
                        // if the hit is a PC, we don't want to set impact to true
                        if (hitTarget is not PlayerCharacter pc)
                        {
                            impactHit.IsAssigned = true;
                        }

                        impactHit.HitObject = gameObjectHit;
                        impactHit.HitTarget = hitTarget;
                        impactHit.ProjectilePosition = queryShape.position;
                        impactHit.ImpactPoint = hitResult.HitPoint;
                        impactHit.HitNormal = hitResult.Normal;

                        hitDatas.Add(impactHit);
                        continue;
                    }
                }

                // Check for overlap layer
                if ((overlapLayer.value & (1 << gameObjectHit.layer)) != 0)
                {
                    FPhysicsHitData hitData = new FPhysicsHitData
                    {
                        IsAssigned = true,
                        HitObject = gameObjectHit,
                        HitTarget = hitTarget,
                        ProjectilePosition = queryShape.position,
                        ImpactPoint = hitResult.HitPoint,
                        HitNormal = hitResult.Normal
                    };

                    hitDatas.Add(hitData);
                }
            }

            // Release hitResults
            ListPool<FOverlapHit>.Release(hitResults);
        }

        public static List<FOverlapHit> GetOverlapHits(Projectile projectile, ref FQueryShape queryShape)
        {
            ProjectileDefinition definition = projectile.Definition;
            Vector3 position = queryShape.position;
            Quaternion rotation = queryShape.rotation;

            List<FOverlapHit> hitResults = ListPool<FOverlapHit>.Get();

            float maxDistance = Vector3.Distance(position, projectile.TargetPosition);

            switch (queryShape.shapeType)
            {
                case EShapeType.Raycast:
                    int rayHitCount = Physics.RaycastNonAlloc(
                        position,
                        rotation * Vector3.forward,
                        rayHitsPool,
                        Mathf.Min(queryShape.shapeExtents.x, maxDistance),
                        definition.OverlapCollisionLayer);

                    hitResults.Capacity = Mathf.Max(hitResults.Capacity, rayHitCount);
                    for (int i = 0; i < rayHitCount; i++)
                    {
                        var hit = rayHitsPool[i];
                        hitResults.Add(new FOverlapHit
                        {
                            GameObject = hit.collider.gameObject,
                            Collider = hit.collider,
                            HitPoint = hit.point,
                            Normal = hit.normal,
                            Distance = hit.distance
                        });
                    }
                    break;

                case EShapeType.Capsule:
                    int capsuleHitCount = Physics.SphereCastNonAlloc(
                        position,
                        queryShape.shapeExtents.x,
                        rotation * Vector3.forward,
                        sphereHitsPool,
                        Mathf.Min(queryShape.shapeExtents.y, maxDistance),
                        definition.OverlapCollisionLayer);

                    hitResults.Capacity = Mathf.Max(hitResults.Capacity, capsuleHitCount);
                    for (int i = 0; i < capsuleHitCount; i++)
                    {
                        var hit = sphereHitsPool[i];
                        hitResults.Add(new FOverlapHit
                        {
                            GameObject = hit.collider.gameObject,
                            Collider = hit.collider,
                            HitPoint = hit.point,
                            Normal = hit.normal,
                            Distance = hit.distance
                        });
                    }
                    break;

                case EShapeType.Sphere:
                    float coneAngleDegreesHalf = queryShape.shapeExtents.y;
                    float coneAngleCos = Mathf.Cos(coneAngleDegreesHalf * Mathf.Deg2Rad);
                    Vector3 coneForward = rotation * Vector3.forward;

                    int hitCount = Physics.OverlapSphereNonAlloc(
                        position,
                        queryShape.shapeExtents.x,
                        collidersPool,
                        definition.OverlapCollisionLayer);

                    hitResults.Capacity = Mathf.Max(hitResults.Capacity, hitCount);
                    for (int i = 0; i < hitCount; i++)
                    {
                        Collider hit = collidersPool[i];
                        Vector3 hitPoint;
                        Vector3 normal;
                        float distance;

                        bool supportsClosestPoint = hit is BoxCollider || hit is SphereCollider || hit is CapsuleCollider ||
                                                   (hit is MeshCollider meshCollider && meshCollider.convex);

                        if (supportsClosestPoint)
                        {
                            hitPoint = hit.ClosestPoint(position);
                        }
                        else
                        {
                            Bounds bounds = hit.bounds;
                            hitPoint = bounds.ClosestPoint(position);
                            if (Vector3.Distance(hitPoint, position) < Mathf.Epsilon)
                            {
                                hitPoint = bounds.center;
                            }
                        }

                        Vector3 toHit = hitPoint - position;
                        distance = toHit.magnitude;

                        Vector3 toHitNormalized;
                        if (distance < Mathf.Epsilon)
                        {
                            toHitNormalized = coneForward;
                            distance = 0.0001f;
                            normal = coneForward;
                        }
                        else
                        {
                            toHitNormalized = toHit / distance;
                            normal = toHitNormalized;
                        }

                        if (queryShape.shapeExtents.y > 0)
                        {
                            float cosAngle = Vector3.Dot(coneForward, toHitNormalized);
                            if (cosAngle < coneAngleCos)
                                continue;
                        }

                        normal = toHitNormalized;

                        hitResults.Add(new FOverlapHit
                        {
                            GameObject = hit.gameObject,
                            Collider = hit,
                            HitPoint = hitPoint,
                            Normal = normal,
                            Distance = distance
                        });
                    }
                    break;
            }

            if (hitResults.Count == 0)
                return hitResults; // Caller must release

            // In-place filtering to keep only the first hit per GameObject
            hitResults.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            seenGameObjectsPool.Clear();
            int writeIndex = 0;
            for (int i = 0; i < hitResults.Count; i++)
            {
                GameObject gameObjectHit = hitResults[i].GameObject;
                if (gameObjectHit != null && !seenGameObjectsPool.Contains(gameObjectHit))
                {
                    hitResults[writeIndex] = hitResults[i];
                    seenGameObjectsPool.Add(gameObjectHit);
                    writeIndex++;
                }
            }
            hitResults.RemoveRange(writeIndex, hitResults.Count - writeIndex);

            return hitResults; // Caller must release
        }

        public static List<FOverlapHit> GetCastHits(Projectile projectile, ref FQueryShape queryShape)
        {
            Vector3 lastPosition = queryShape.lastPostion;
            ProjectileDefinition definition = projectile.Definition;
            Vector3 nextPosition = queryShape.position;
            Quaternion rotation = queryShape.rotation;
            Vector3 direction = nextPosition - lastPosition;
            float distance = direction.magnitude;

            List<FOverlapHit> hitResults = ListPool<FOverlapHit>.Get();

            if (distance < Mathf.Epsilon)
                return hitResults; // Caller must release

            Vector3 directionNormalized = direction.normalized;

            switch (queryShape.shapeType)
            {
                case EShapeType.Raycast:
                    Ray ray = new Ray(lastPosition, directionNormalized);
                    int raycastHitCount = Physics.RaycastNonAlloc(
                        ray,
                        rayHitsPool,
                        distance,
                        definition.OverlapCollisionLayer);

                    hitResults.Capacity = Mathf.Max(hitResults.Capacity, raycastHitCount);
                    for (int i = 0; i < raycastHitCount; i++)
                    {
                        RaycastHit hit = rayHitsPool[i];
                        Collider hitCollider = hit.collider;

                        // Fallback: if point is zero, compute closest point manually
                        if (hit.point == Vector3.zero)
                        {
                            if (hit.collider != null)
                                hit.point = hit.collider.ClosestPoint(queryShape.position);
                        }

                        hitResults.Add(new FOverlapHit
                        {
                            Collider = hitCollider,
                            GameObject = hitCollider.gameObject,
                            HitPoint = hit.point,
                            Normal = hit.normal,
                            Distance = hit.distance
                        });
                    }
                    break;

                case EShapeType.Sphere:
                    int hitCount = Physics.SphereCastNonAlloc(
                        lastPosition,
                        queryShape.shapeExtents.x,
                        directionNormalized,
                        sphereHitsPool,
                        distance,
                        definition.OverlapCollisionLayer);

                    hitResults.Capacity = Mathf.Max(hitResults.Capacity, hitCount);
                    for (int i = 0; i < hitCount; i++)
                    {
                        RaycastHit hit = sphereHitsPool[i];
                        Collider hitCollider = hit.collider;

                        // Fallback: if point is zero, compute closest point manually
                        if (hit.point == Vector3.zero)
                        {
                            if (hit.collider != null)
                                hit.point = hit.collider.ClosestPoint(queryShape.position);
                        }

                        hitResults.Add(new FOverlapHit
                        {
                            Collider = hitCollider,
                            GameObject = hitCollider.gameObject,
                            HitPoint = hit.point,
                            Normal = hit.normal,
                            Distance = hit.distance
                        });
                    }
                    break;
            }

            if (hitResults.Count == 0)
                return hitResults; // Caller must release

            // In-place filtering to keep only the first hit per GameObject
            hitResults.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            seenGameObjectsPool.Clear();
            int writeIndex = 0;
            for (int i = 0; i < hitResults.Count; i++)
            {
                GameObject gameObjectHit = hitResults[i].GameObject;
                if (gameObjectHit != null && !seenGameObjectsPool.Contains(gameObjectHit))
                {
                    hitResults[writeIndex] = hitResults[i];
                    seenGameObjectsPool.Add(gameObjectHit);
                    writeIndex++;
                }
            }
            hitResults.RemoveRange(writeIndex, hitResults.Count - writeIndex);

            return hitResults; // Caller must release
        }

        public static float GetScaleAtTime(ProjectileDefinition definition, float timeSinceFire, float networkDeltaTime)
        {
            if (definition == null)
                return 1f;

            AnimationCurveAsset animationCurveDefinition = definition.ScaleOverLifetime;
            if (animationCurveDefinition == null)
                return 1f;

            float percentLifetimeExpired = Mathf.Clamp01(timeSinceFire / (definition.LifetimeTicks * networkDeltaTime));
            return animationCurveDefinition.Curve.Evaluate(percentLifetimeExpired);
        }

        public static bool IsImpactObjectValid(Projectile projectile, GameObject hitObject, IHitTarget hitTarget)
        {
            if (projectile == null || projectile.Instigator == null || hitTarget == projectile.Instigator)
                return false;

            // if this is called from fixed update projectile

            if (projectile.IsNPCProjectile)
            {
                if (projectile is FixedUpdateProjectile)
                {
                    if(hitTarget is PlayerCharacter pc)
                        if(!pc.HasStateAuthority)
                            return false;
                }
            }

            return true;
        }

        public static bool IsCollidingActive(ProjectileDefinition definition, float simTimeSinceFired)
        {
            if (definition.CollisionCheckTrim.x != 0.0f && definition.CollisionCheckTrim.x > simTimeSinceFired)
                return false;

            if (definition.CollisionCheckTrim.y != 0.0f && simTimeSinceFired > definition.CollisionCheckTrim.y)
                return false;

            return true;
        }

        public static void CheckProximityFuse(ref FProjectileData data, FixedUpdateProjectile projectile, ProjectileDefinition definition, int tick)
        {
            FQueryShape queryShape = new FQueryShape
            {
                position = projectile.Position,
                rotation = projectile.Rotation,
                shapeType = EShapeType.Sphere,
                shapeExtents = new Vector3(definition.ProximityDetonationRange, 0f, 0f)
            };

            List<FOverlapHit> hitResults = ListPool<FOverlapHit>.Get();
            hitResults = GetOverlapHits(projectile, ref queryShape);

            // Check if the proximity target is valid (
            foreach (var hitResult in hitResults)
            {
                GameObject gameObjectHit = hitResult.GameObject;

                IHitTarget hitTarget = null;

                if (gameObjectHit.tag == "Hurtbox")
                {
                    HurtboxOwner hitboxOwnerComp = gameObjectHit.GetComponent<HurtboxOwner>();
                    if (hitboxOwnerComp == null)
                        continue;

                    hitTarget = hitboxOwnerComp.HitTarget;

                    if (hitTarget is PlayerCharacter pc)
                    { 
                        data.IsProximityFuseActive = true;
                        projectile.FuseDetonationTick = tick + definition.ProximityDetonationTicks;
                    }
                }
            }

            ListPool<FOverlapHit>.Release(hitResults);
        }
    }
}