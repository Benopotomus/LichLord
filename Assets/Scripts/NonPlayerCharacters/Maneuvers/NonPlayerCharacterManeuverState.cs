using LichLord.Projectiles;
using LichLord.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public class NonPlayerCharacterManeuverState
    {
        public NonPlayerCharacterManeuverDefinition Definition;
        public ENPCState ActiveState = ENPCState.Maneuver_1;
        public int CooldownExpirationTick;
        public int ActivationExpirationTick;
        public int ActivationTick;
        public float RandomAimOffset = 7f;
        public bool IsEnabled = true;

        public bool IsValid()
        {
            if (Definition == null)
                return false;

            return true;
        }

        public bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            if (!IsEnabled)
                return false;

            if (Definition == null) 
                return false;
            
            if (IsOnCooldown(tick))
                return false;

            bool canBeSelect = Definition.CanBeSelected(brainComponent, tick);

            return canBeSelect;
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
            NonPlayerCharacterRuntimeState runtimeState, 
            int tick)
        {
            var oldState = runtimeState.GetState();

            if (oldState != ENPCState.Idle)
                return false;

            if(IsOnCooldown(tick)) 
                return false;

            npc.MeleeHitTracker.HitsPerSwing.Clear();

            runtimeState.SetState(ActiveState);

            int currentAnimIndex = runtimeState.GetAnimationIndex();
            int newAnimIndex = UnityEngine.Random.Range(0, Definition.AnimationTriggers.Count);

            // If the new index is the same as the current, increment and wrap around
            if (newAnimIndex == currentAnimIndex)
            {
                newAnimIndex = (currentAnimIndex + 1) % Definition.AnimationTriggers.Count;
            }

            runtimeState.SetAnimationIndex(newAnimIndex);

            ActivationTick = tick;
            CooldownExpirationTick = ActivationTick + Definition.CooldownTicks;
            ActivationExpirationTick = ActivationTick + Definition.StateTicks;

            return true;
        }

        public void UpdateManeuverTick(NonPlayerCharacter npc, int tick)
        {
            int ticksSinceStart = tick - ActivationTick;

            if (Definition is NonPlayerCharacterAttackManeuverDefinition attackManeuver)
            {
                List<FManeuverProjectile> projectiles = attackManeuver.ManeuverProjectiles;

                for (int i = 0; i < projectiles.Count; i++)
                {
                    FManeuverProjectile projectile = projectiles[i];

                    if (projectile.SpawnTick == ticksSinceStart)
                    {
                        SpawnProjectile(npc, projectile, tick);
                    }
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
            IChunkTrackable target = npc.Brain.AttackTarget.Target;

            IHitTarget hitTarget = npc.Brain.AttackTarget.Target as IHitTarget;

            Vector3 targetPos = target.Position;
            targetPos.y += (1f + Definition.VerticalAimOffset);

            // Apply aim offset here
            if (projectileData.AimOffset != Vector2.zero)
            {
                Vector3 dir = (targetPos - muzzlePosition).normalized;
                Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
                if (right.sqrMagnitude < 0.001f) right = Vector3.right;

                Vector3 up = Vector3.Cross(right, dir);

                float dist = Vector3.Distance(muzzlePosition, targetPos);

                targetPos += right * projectileData.AimOffset.x * dist;
                targetPos += up * projectileData.AimOffset.y * dist;
            }

            // Modify target position by target velocity
            if (target is PlayerCharacter pc)
            {
                bool targetIsMasterClient = pc.HasStateAuthority;

                targetPos = CalculateLeadTargetPosition(muzzlePosition,
                    targetPos,
                    pc.Movement.WorldVelocity, 
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
                hitTarget,
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
            targetVelocity.y = 0f;

            float distance = Vector3.Distance(targetPosition, shooterPosition);
            float interceptTime = distance / 10f;

            Vector3 additiveTarget = (targetVelocity * interceptTime);

            return targetPosition + (targetVelocity * interceptTime);
        }

        private Vector3 CalculateRandomOffset(Vector3 muzzlePosition, Vector3 targetPosition)
        {
            // distance from muzzle to target
            float targetDistance = Vector3.Distance(muzzlePosition, targetPosition);

            // how much to randomize (in meters at target distance)
            float randomRadius = targetDistance * Mathf.Tan(RandomAimOffset * Mathf.Deg2Rad);

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
