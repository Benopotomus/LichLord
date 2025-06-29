using LichLord.Projectiles;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public class NonPlayerCharacterManeuverState
    {
        public NonPlayerCharacterManeuverDefinition Definition;
        public ENonPlayerState ActiveState = ENonPlayerState.Maneuver_1;
        public int CooldownExpirationTick;
        public int ActivationExpirationTick;
        public int ActivationTick;

        public bool IsValid()
        {
            if (Definition == null)
                return false;

            return true;
        }

        public bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            if(Definition == null) 
                return false;
            
            if (IsOnCooldown(tick))
                return false;

            if(Definition.RequiresEnemyTarget && brainComponent.AttackTarget == null)
                return false;

            return true;
        }

        public bool IsOnCooldown(int tick)
        {
            return CooldownExpirationTick > tick;
        }

        public bool HasExpired(int tick)
        { 
            return ActivationExpirationTick < tick;
        }

        public bool ExecuteManeuver(NonPlayerCharacter npc, 
            ref FNonPlayerCharacterData data, 
            int tick)
        {
            if (data.State != ENonPlayerState.Idle)
                return false;

            if(IsOnCooldown(tick)) 
                return false;

            data.State = ActiveState;

            int currentAnimIndex = data.AnimationIndex;
            int newAnimIndex = UnityEngine.Random.Range(0, Definition.AnimationTriggers.Count);

            // If the new index is the same as the current, increment and wrap around
            if (newAnimIndex == currentAnimIndex)
            {
                newAnimIndex = (currentAnimIndex + 1) % Definition.AnimationTriggers.Count;
            }

            data.AnimationIndex = newAnimIndex;

            npc.Replicator.UpdateNPCData(ref data);
            ActivationTick = tick;
            CooldownExpirationTick = ActivationTick + Definition.CooldownTicks;
            ActivationExpirationTick = ActivationTick + Definition.StateTicks;

            return true;
        }

        public void UpdateManeuverTick(NonPlayerCharacter npc,
            ref FNonPlayerCharacterData data,
            int tick)
        {
            int ticksSinceStart = tick - ActivationTick;

            List<FManeuverProjectile> projectiles = Definition.ManeuverProjectiles;

            for (int i = 0; i < projectiles.Count; i++)
            {
                FManeuverProjectile projectile = projectiles[i];

                if (projectile.SpawnTick == ticksSinceStart)
                {
                    SpawnProjectile(npc, projectile, tick);
                }
            }
        }

        private void SpawnProjectile(NonPlayerCharacter npc, FManeuverProjectile projectileData, int tick)
        {
            ProjectileManager projectileManager = npc.Context.ProjectileManager;
            if (projectileManager == null)
                return;

            ProjectileDefinition definition = projectileData.Definition;

            Vector3 muzzlePosition = MuzzleUtility.GetMuzzlePosition(npc, projectileData.Muzzle);

            Vector3 targetPos = npc.Brain.AttackTarget.Position;
            targetPos.y += 1f;

            // Modify target position by target velocity
            if (npc.Brain.AttackTarget is PlayerCharacter pc)
            {
                bool targetIsMasterClient = pc.HasStateAuthority;

                targetPos = CalculateLeadTargetPosition(muzzlePosition,
                    targetPos,
                    pc.Movement.Velocity, 
                    definition.Speed,
                    targetIsMasterClient);
            }

            targetPos = CalculateRandomOffset(muzzlePosition, targetPos);

            // Modify for thrown
            targetPos = GetProjectileTargetPosition(targetPos, muzzlePosition, definition);

            FProjectileFireEvent fireEvent = new FProjectileFireEvent();
            FProjectilePayload payload = new FProjectilePayload();
            FProjectilePayload payload_spawnedProjectile = new FProjectilePayload();
            payload.damagePotential.DamageValue = projectileData.Damage.DamageValue;
            payload.damagePotential.DamageType = projectileData.Damage.DamageType;

            payload_spawnedProjectile.damagePotential.DamageValue = projectileData.Damage.DamageValue;
            payload_spawnedProjectile.damagePotential.DamageType = projectileData.Damage.DamageType;

            ProjectileManager.CreateProjectileFireEvent(
                ref fireEvent,
                definition,
                npc,
                new FNetObjectID(),
                muzzlePosition,
                targetPos,
                tick,
                ref payload,
                ref payload_spawnedProjectile
            );

            var projectile = projectileManager.SpawnProjectile(fireEvent); 
        }

        private Vector3 CalculateLeadTargetPosition(
            Vector3 shooterPosition,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            float projectileSpeed,
            bool targetIsMasterClient
        )
        {
            float maxLead = targetIsMasterClient ? 0.1f : 0.2f;
            float distance = Vector3.Distance(targetPosition, shooterPosition);
            float distancePercent = Mathf.Clamp01((distance / projectileSpeed) + 0.25f);

            float interceptTime = Mathf.Clamp(distance / projectileSpeed, 0, maxLead * distancePercent);
            Vector3 additiveTarget = (targetVelocity * interceptTime);
            additiveTarget.y = 0;

            return targetPosition + (targetVelocity * interceptTime);
        }

        private Vector3 CalculateRandomOffset(Vector3 muzzlePosition, Vector3 targetPosition)
        {
            // distance from muzzle to target
            float targetDistance = Vector3.Distance(muzzlePosition, targetPosition);

            // how much to randomize (in meters at target distance)
            float randomRadius = targetDistance * Mathf.Tan(5 * Mathf.Deg2Rad);

            // random point in circle around target
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * randomRadius;

            // apply the random offset on the XZ plane
            targetPosition.x += randomCircle.x;
            targetPosition.z += randomCircle.y;

            return targetPosition;
        }

        private Vector3 GetProjectileTargetPosition(Vector3 currentTargetPos, Vector3 muzzlePosition, ProjectileDefinition definition)
        { 

            ProjectileMovement projectileMovement = definition.ProjectileMovement;

            if (projectileMovement is ThrownMovement thrownMovement)
            {
                return thrownMovement.GetOffsetTargetPosition(muzzlePosition, currentTargetPos, definition.Speed);
            }

            return currentTargetPos;
        }
    }
}
