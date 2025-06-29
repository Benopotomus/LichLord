namespace LichLord.Projectiles
{
    using UnityEngine;

    [CreateAssetMenu(menuName = "LichLord/Projectiles/ThrownMovement")]
    public class ThrownMovement : ProjectileMovement
    {
        [SerializeField]
        private float _gravity;
        private float Gravity => _gravity;

        // RENDER

        public override void OnRender(RenderProjectile projectile,
            ref FProjectileData toData,
            ref FProjectileData fromData,
            float bufferAlpha,
            float deltaTime,
            float renderTimeSinceFired,
            int tick)
        {
            Vector3 lastPosition = projectile.Position;

            Vector3 newRenderTargetPosition = GetLinearMovePosition(
                projectile.Definition,
                ref toData,
                renderTimeSinceFired);

            if (toData.HasImpacted)
            {
                newRenderTargetPosition = ClampToImpactPosition(lastPosition, 
                    deltaTime, 
                    ref toData, 
                    projectile.Definition);
            }

            projectile.Position = newRenderTargetPosition;
            projectile.Velocity = (projectile.Position - lastPosition) / Time.deltaTime;
            projectile.Rotation = GetRotation(projectile.Definition,
                ref toData,
                toData.TargetPosition.Position,
                toData.Position.Position,
                projectile.Velocity,
                projectile.Rotation);
        }

        private Vector3 ClampToImpactPosition(
            Vector3 lastPosition,
            float deltaTime,
            ref FProjectileData toData,
            ProjectileDefinition definition)
        {
            return Vector3.Lerp(lastPosition, toData.TargetPosition.Position, deltaTime * definition.Speed);
        }

        private Vector3 GetLinearMovePosition(ProjectileDefinition definition,
            ref FProjectileData toData,
            float timeSinceFired)
        {
            if (toData.HasImpacted)
            {
                return toData.TargetPosition.Position;
            }

            if (timeSinceFired <= 0f)
                return toData.Position.Position;

            // Calculate direction to offset target position
            Vector3 direction = Vector3CompressedExtensions.SubtractAndNormalize(toData.TargetPosition.Position, toData.Position.Position);
            Vector3 velocity = direction * definition.Speed;

            // Calculate position, applying gravity from the initial y-position
            Vector3 newPosition = toData.Position.Position + (velocity * timeSinceFired);
            newPosition.y = toData.Position.Position.y + (velocity.y * timeSinceFired) + 0.5f * -Gravity * timeSinceFired * timeSinceFired;

            return newPosition;
        }

        public override Vector3 GetInitialVelocity(ProjectileDefinition definition,
            Vector3 targetPosition,
            Vector3 spawnPosition)
        {
            Vector3 direction = Vector3CompressedExtensions.SubtractAndNormalize(targetPosition, spawnPosition);
            return direction * definition.Speed;
        }

        // FIXED UPDATE

        public override void OnFixedUpdate(FixedUpdateProjectile projectile,
            ref FProjectileData data,
            int tick,
            float simulationTime,
            float deltaTime)
        {
            if (data.HasImpacted)
                return;

            ProjectileDefinition definition = projectile.Definition;

            float lastTimeSinceFired = ((tick - data.FireTick) - 1) * deltaTime;
            float newTimeSinceFired = (tick - data.FireTick) * deltaTime;
            Vector3 oldPosition = GetLinearMovePosition(definition, ref data, lastTimeSinceFired);
            Vector3 newPosition = GetLinearMovePosition(definition, ref data, newTimeSinceFired);

            Vector3 newVelocity = (newPosition - oldPosition) / deltaTime;

            Quaternion oldRotation = projectile.Rotation;
            Quaternion newRotation = GetRotation(
                projectile.Definition,
                ref data,
                data.TargetPosition.Position,
                data.Position.Position,
                newVelocity,
                projectile.Rotation);

            ProjectilePhysicsUtility.CheckAndHandleCollision(projectile,
                ref data,
                tick,
                simulationTime,
                deltaTime,
                oldPosition,
                newPosition,
                oldRotation,
                newRotation);

            projectile.Position = newPosition;
            projectile.Velocity = newVelocity;
            projectile.Rotation = newRotation;
        }

        // Helpers

        public Vector3 GetOffsetTargetPosition(Vector3 shooterPosition, Vector3 originalTargetPosition, float speed)
        {
            Vector3 launchVelocity;
            bool found = SolveBallisticArc(
                shooterPosition,
                originalTargetPosition,
                speed,
                Gravity,
                out launchVelocity,
                preferHighArc: false);

            if (!found)
            {
                // If no ballistic solution, try raising the target Y by a dynamic amount:
                float distance = Vector3.Distance(shooterPosition, originalTargetPosition);

                // Raise the apex proportionally to distance
                float apexOffset = distance * 0.75f;

                Vector3 fallbackTarget = originalTargetPosition;
                fallbackTarget.y += apexOffset;

                return fallbackTarget;
            }

            // Compute how long the *straight-line* travel would take to the true target
            float distanceToTarget = Vector3.Distance(shooterPosition, originalTargetPosition);
            float timeToTarget = distanceToTarget / speed;

            // Project the offset target in the launch direction so your current linear code will hit it
            Vector3 offsetTarget = shooterPosition + launchVelocity.normalized * speed * timeToTarget;

            return offsetTarget;
        }

        private bool SolveBallisticArc(
            Vector3 shooterPosition,
            Vector3 targetPosition,
            float launchSpeed,
            float gravity,
            out Vector3 launchVelocity,
            bool preferHighArc = false)
        {
            launchVelocity = Vector3.zero;

            Vector3 delta = targetPosition - shooterPosition;
            Vector3 deltaXZ = new Vector3(delta.x, 0f, delta.z);
            float horizontalDistance = deltaXZ.magnitude;
            float verticalDistance = delta.y;

            float speedSquared = launchSpeed * launchSpeed;
            float gravityMagnitude = Mathf.Abs(gravity);

            float underSqrt = speedSquared * speedSquared
                - gravityMagnitude * (gravityMagnitude * horizontalDistance * horizontalDistance
                + 2 * verticalDistance * speedSquared);

            if (underSqrt < 0f)
            {
                return false;
            }

            float sqrt = Mathf.Sqrt(underSqrt);

            float angleLow = Mathf.Atan2(speedSquared - sqrt, gravityMagnitude * horizontalDistance);
            float angleHigh = Mathf.Atan2(speedSquared + sqrt, gravityMagnitude * horizontalDistance);

            float chosenAngle = preferHighArc ? angleHigh : angleLow;

            Vector3 directionXZ = deltaXZ.normalized;

            launchVelocity = directionXZ * Mathf.Cos(chosenAngle) * launchSpeed
                           + Vector3.up * Mathf.Sin(chosenAngle) * launchSpeed;

            return true;
        }

    }
}