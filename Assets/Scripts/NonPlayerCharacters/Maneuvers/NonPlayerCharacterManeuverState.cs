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


            Vector3 targetPos = npc.Brain.AttackTarget.Position;
            targetPos.y += 1f;

            Vector3 muzzlePosition = MuzzleUtility.GetMuzzlePosition(npc, projectileData.Muzzle);

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
            //Debug.Log($"[GunActionData] Fired projectile with {ActionName} using ProjectileManager");
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
