using UnityEngine;
using Fusion;
using LichLord.Projectiles;

namespace LichLord
{
    [CreateAssetMenu(fileName = "GunManeuverDefinition", menuName = "LichLord/Maneuvers/GunManeuverDefinition", order = 2)]
    public class GunManeuverDefinition : ManeuverDefinition
    {
        [SerializeField] private ProjectileDefinition projectileDefinition; // For ProjectileManager

        public override void SelectAction(PlayerCharacter playerCreature, NetworkRunner runner)
        {
        }

        public override void DeselectAction(PlayerCharacter playerCreature, NetworkRunner runner)
        {
        }

        public override void StartExecute(PlayerCharacter playerCreature, NetworkRunner runner)
        {
            // Get the action spawn point
            Transform spawnPoint = playerCreature.Maneuvers.ActionSpawnPoint != null ? playerCreature.Maneuvers.ActionSpawnPoint : playerCreature.transform;
            Vector3 hitPosition = Vector3.zero;
            Vector3 hitNormal = Vector3.zero;

            Vector3 targetPos = playerCreature.Context.Camera.CachedRaycastHit.position;

            // Spawn projectile if projectileDefinition is set
            if (projectileDefinition != null)
            {
                ProjectileManager projectileManager = playerCreature.Context.ProjectileManager;
                if (projectileManager != null)
                {
                    FProjectileFireEvent fireEvent = new FProjectileFireEvent();
                    FProjectilePayload payload = new FProjectilePayload();
                    FProjectilePayload payload_spawnedProjectile = new FProjectilePayload();

                    ProjectileManager.CreateProjectileFireEvent(
                        ref fireEvent,
                        projectileDefinition,
                        playerCreature,
                        new FNetObjectID(),
                        spawnPoint.position,
                        targetPos,
                        runner.Tick,
                        ref payload,
                        ref payload_spawnedProjectile
                    );

                    var projectile = projectileManager.SpawnProjectile(fireEvent);
                    //Debug.Log($"[GunActionData] Fired projectile with {ActionName} using ProjectileManager");
                }
                else
                {
                    Debug.LogWarning($"[GunActionData] Cannot fire projectile for {ManeuverName}: ProjectileManager not found.");
                }
            }


            // Trigger RPC via CreatureActions
            if (playerCreature.Maneuvers != null)
            {
                playerCreature.Maneuvers.RPC_ExecuteGunAction(playerCreature.Object.Id, TableID, spawnPoint.position, targetPos, hitPosition, hitNormal);
            }
            
        }
    }
}