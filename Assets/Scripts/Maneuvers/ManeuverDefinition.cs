using Fusion;
using LichLord.Projectiles;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{

    [CreateAssetMenu(fileName = "Maneuver", menuName = "LichLord/Maneuvers/ManeuverDefinition", order = 1)]
    public class ManeuverDefinition : TableObject
    {
        public string ManeuverName;

        public float Duration = 1f;
        public float Cooldown = 1f;

        public AudioClip ActionSound; // Sound played when performing action (e.g., FireSound for gun)
        public VisualEffectBase ActionEffect; // VFX played when performing action (e.g., MuzzleParticle for gun)

        [Header ("Animation")]
        public bool Fullbody; // Animator trigger (e.g., "Shoot" for gun)
        public int AnimationTriggerNumber;
        public float AnimationSpeed = 1f;


        public float MovementSpeedMultiplier = 1f; // Scales movement speed during action

        [SerializeField]
        private List<FManeuverProjectile> _timedProjectiles = new List<FManeuverProjectile>();

        [SerializeField]
        private List<FManeuverProjectile> _sustainedProjectiles = new List<FManeuverProjectile>();

        public virtual void SelectAction(PlayerCharacter playerCreature, NetworkRunner runner) { }

        public virtual void DeselectAction(PlayerCharacter playerCreature, NetworkRunner runner) { }

        public virtual void StartExecute(PlayerCharacter playerCharacter, NetworkRunner runner) 
        {
            playerCharacter.Maneuvers.RPC_NotifyActionExecution((ushort)TableID);
        }

        public virtual void SustainExecute(PlayerCharacter playerCharacter, NetworkRunner runner, int ticksSinceStart)
        {
            for (int i = 0; i < _timedProjectiles.Count; i++)
            { 
                var projectile  = _timedProjectiles[i];
                if (projectile.SpawnTick == ticksSinceStart)
                {
                    SpawnProjectile(playerCharacter, ref projectile, runner.Tick);
                }
            }
        }

        public virtual void EndExecute(PlayerCharacter playerCreature, NetworkRunner runner) { }

        private void SpawnProjectile(PlayerCharacter playerCharacter, ref FManeuverProjectile projectileData, int tick)
        {
            ProjectileManager projectileManager = playerCharacter.Context.ProjectileManager;
            if (projectileManager == null)
                return;

            Vector3 targetPos = playerCharacter.Context.Camera.CachedRaycastHit.position;

            FProjectileFireEvent fireEvent = new FProjectileFireEvent();
            FProjectilePayload payload = new FProjectilePayload();
            FProjectilePayload payload_spawnedProjectile = new FProjectilePayload();

            ProjectileManager.CreateProjectileFireEvent(
                ref fireEvent,
                projectileData.Definition,
                playerCharacter,
                new FNetObjectID(),
                playerCharacter.GetMuzzlePosition(projectileData.Muzzle),
                targetPos,
                tick,
                ref payload,
                ref payload_spawnedProjectile
            );

            var projectile = projectileManager.SpawnProjectile(fireEvent);
            //Debug.Log($"[GunActionData] Fired projectile with {ActionName} using ProjectileManager");
        }
    }
}