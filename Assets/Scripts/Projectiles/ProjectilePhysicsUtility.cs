namespace LichLord.Projectiles
{
    using DWD.AnimationCurveAsset;
    using Fusion;
    using System.Collections.Generic;
    using UnityEngine;

    public static class ProjectilePhysicsUtility
    {
        // This needs to be on the render thread since we want projectiles that are not from players
        // to affect players from their point of view
        public static void CheckAndHandleCollision(Projectile projectile,
            ref FProjectileData data,
            int tick,
            float simulationTime,
            float localDeltaTime,
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
                    PerformCollisionChecks(projectile, ref data, tick, simTimeSinceFired,
                        oldPosition, newPosition,
                        oldRotation, newRotation);
                }
            }

        }

        private static void PerformCollisionChecks(Projectile projectile,
            ref FProjectileData data,
            int tick,
            float simTimeSinceFired,
            Vector3 oldPosition,
            Vector3 newPosition,
            Quaternion oldRotation,
            Quaternion newRotation)
        {
            ProjectileDefinition definition = projectile.Definition;

            FPhysicsHitData impactHit = new FPhysicsHitData();
            List<FPhysicsHitData> hitDatas = new List<FPhysicsHitData>();

            data.IsReflected = false;

            // Perform the shape collision check
            CheckShapeCollisions(projectile,
                ref data,
                tick,
                simTimeSinceFired,
                newPosition,
                newRotation,
                ref hitDatas,
                ref impactHit);

            if (definition.OnlyAffectImpactTarget && impactHit.IsAssigned)
            {
                hitDatas = new List<FPhysicsHitData> { impactHit };
                projectile.UpdateAffectedActors(ref data, hitDatas, tick);
            }
            else
            {
                projectile.UpdateAffectedActors(ref data, hitDatas, tick);
            }

            // If there's an impact, handle it
            if (impactHit.IsAssigned)
            {
                ProjectileImpactUtility.HandleImpact(projectile, ref data, ref impactHit, tick);
            }
            
        }

        public static void CheckShapeCollisions(Projectile projectile,
            ref FProjectileData data,
            int tick,
            float simTimeSinceFired,
            Vector3 position,
            Quaternion rotation,
            ref List<FPhysicsHitData> hitDatas,
            ref FPhysicsHitData impactHit)
                {
                    ProjectileDefinition definition = projectile.Definition;

                    FQueryShape _currentQueryShape = new FQueryShape();

                    _currentQueryShape.position = position;
                    _currentQueryShape.rotation = rotation;
                    _currentQueryShape.shapeType = definition.Shape;
                    _currentQueryShape.shapeExtents = definition.Extents * GetScaleAtTime(definition, simTimeSinceFired);

            GetValidHits(projectile, ref data, ref _currentQueryShape, ref hitDatas, ref impactHit);
        }

        public static void GetValidHits(Projectile projectile,
            ref FProjectileData data,
            ref FQueryShape queryShape,
            ref List<FPhysicsHitData> hitDatas,
            ref FPhysicsHitData impactHit)
        {

            List<FOverlapHit> hitResults;
            if(projectile.Position != queryShape.position) 
                hitResults = GetCastHits(projectile, ref queryShape);
            else
                hitResults = GetOverlapHits(projectile, ref queryShape);

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
                        impactHit.IsAssigned = true;
                        impactHit.HitObject = gameObjectHit;
                        impactHit.HitTarget = hitTarget;
                        impactHit.ProjectilePosition = queryShape.position;
                        //impactHit.ImpactVelocity = projectile.FixedUpdateVelocity;
                        impactHit.ImpactPoint = hitResult.HitPoint;
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
                    //hitData.ImpactVelocity = projectile.FixedUpdateVelocity;
                    hitData.ImpactPoint = hitResult.HitPoint;
                    impactHit.HitNormal = hitResult.Normal;

                    hitDatas.Add(hitData);
                }
            }
        }

        public struct FOverlapHit
        {
            public GameObject GameObject;
            public Collider Collider;
            public Vector3 HitPoint;
            public Vector3 Normal;
            public float Distance;
        }

        public static List<FOverlapHit> GetOverlapHits(Projectile projectile, ref FQueryShape queryShape)
        {
            ProjectileDefinition definition = projectile.Definition;
            Vector3 position = queryShape.position; // Changed to Vector3 for 3D
            Quaternion rotation = queryShape.rotation;

            List<FOverlapHit> hitResults = new List<FOverlapHit>();

            switch (queryShape.shapeType)
            {
                case EShapeType.Raycast:
                    RaycastHit[] rayHits = Physics.RaycastAll(
                        position,
                        rotation * Vector3.forward, // Changed to Vector3.forward for 3D
                        queryShape.shapeExtents.x,
                        definition.OverlapCollisionLayer);

                    foreach (var hit in rayHits)
                    {
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
                    // Use NonAlloc to avoid allocations; assume reasonable max hits
                    Collider[] collidersHit = new Collider[definition.CollisionCheckNumber]; // Adjust size as needed
                    int hitCount = Physics.OverlapSphereNonAlloc(
                        position,
                        queryShape.shapeExtents.x,
                        collidersHit,
                        definition.OverlapCollisionLayer);

                    for (int i = 0; i < hitCount; i++)
                    {
                        Collider hit = collidersHit[i];
                        Vector3 hitPoint;
                        Vector3 normal;
                        float distance;

                        // Check if collider supports ClosestPoint
                        bool supportsClosestPoint = hit is BoxCollider || hit is SphereCollider || hit is CapsuleCollider ||
                                                   (hit is MeshCollider meshCollider && meshCollider.convex);

                        if (supportsClosestPoint)
                        {
                            // Use ClosestPoint for supported colliders
                            hitPoint = hit.ClosestPoint(position);
                        }
                        else
                        {
                            // Fallback for unsupported colliders
                            Bounds bounds = hit.bounds;
                            hitPoint = bounds.ClosestPoint(position);
                            if (Vector3.Distance(hitPoint, position) < Mathf.Epsilon)
                            {
                                // If bounds.ClosestPoint is invalid, use bounds center
                                hitPoint = bounds.center;
                            }
                        }

                        // Calculate normal and distance
                        normal = (hitPoint - position).normalized;
                        distance = Vector3.Distance(position, hitPoint);

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
                return hitResults;

            // Filter hitResults to keep only the first hit per GameObject
            List<FOverlapHit> filteredHits = new List<FOverlapHit>(hitResults.Count);
            HashSet<GameObject> seenGameObjects = new HashSet<GameObject>();

            foreach (FOverlapHit hit in hitResults)
            {
                GameObject gameObjectHit = hit.GameObject;
                if (gameObjectHit != null && !seenGameObjects.Contains(gameObjectHit))
                {
                    filteredHits.Add(hit);
                    seenGameObjects.Add(gameObjectHit);
                }
            }

            return filteredHits;
        }

        public static List<FOverlapHit> GetCastHits(Projectile projectile, ref FQueryShape queryShape)
        {
            Vector3 lastPosition = projectile.Position;
            ProjectileDefinition definition = projectile.Definition;
            Vector3 nextPosition = queryShape.position;
            Quaternion rotation = queryShape.rotation;
            Vector3 direction = nextPosition - lastPosition;
            float distance = direction.magnitude;

            List<FOverlapHit> hitResults = new List<FOverlapHit>();

            if (distance < Mathf.Epsilon)
                return hitResults; // Skip if no movement

            Vector3 directionNormalized = direction.normalized;

            switch (queryShape.shapeType)
            {
                case EShapeType.Raycast:
                    Ray ray = new Ray(lastPosition, directionNormalized);
                    RaycastHit[] rayHits = new RaycastHit[definition.CollisionCheckNumber];

                    int raycastHitCount = Physics.RaycastNonAlloc(
                        ray,
                        rayHits,
                        distance,
                        definition.OverlapCollisionLayer);

                    for (int i = 0; i < raycastHitCount; i++)
                    {
                        RaycastHit hit = rayHits[i];
                        Collider hitCollider = hit.collider;

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
                    RaycastHit[] sphereHits = new RaycastHit[definition.CollisionCheckNumber];
                    int hitCount = Physics.SphereCastNonAlloc(
                        lastPosition,
                        queryShape.shapeExtents.x,
                        directionNormalized,
                        sphereHits,
                        distance,
                        definition.OverlapCollisionLayer);

                    for (int i = 0; i < hitCount; i++)
                    {
                        RaycastHit hit = sphereHits[i];
                        Collider hitCollider = hit.collider;

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
                return hitResults;

            // Filter hitResults to keep only the first hit per GameObject
            List<FOverlapHit> filteredHits = new List<FOverlapHit>(hitResults.Count);
            HashSet<GameObject> seenGameObjects = new HashSet<GameObject>();

            foreach (FOverlapHit hit in hitResults)
            {
                GameObject gameObjectHit = hit.GameObject;
                if (gameObjectHit != null && !seenGameObjects.Contains(gameObjectHit))
                {
                    filteredHits.Add(hit);
                    seenGameObjects.Add(gameObjectHit);
                }
            }

            return filteredHits;
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

        public static bool IsImpactObjectValid(Projectile projectile, GameObject hitObject, IHitTarget hitTarget)
        {
            // Early exit for untagged or irrelevant objects
            if (projectile == null)
                return false;

            if (projectile.Instigator == null)
                return false;

            if (hitTarget == projectile.Instigator)
                return false;

            return true;
        }
        
        public static bool IsCollidingActive(ProjectileDefinition definition, float simTimeSinceFired)
        {
            // See if it can collide yet.
            if (definition.CollisionCheckTrim.x != 0.0f && definition.CollisionCheckTrim.x > simTimeSinceFired)
                return false;

            if (definition.CollisionCheckTrim.y != 0.0f && simTimeSinceFired > definition.CollisionCheckTrim.y)
                return false;

            return true;
        }
    }
}
