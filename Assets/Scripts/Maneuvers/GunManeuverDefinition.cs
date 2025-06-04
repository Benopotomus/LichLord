using UnityEngine;
using Fusion;
using LichLord.Projectiles;

namespace LichLord
{
    [CreateAssetMenu(fileName = "GunManeuverDefinition", menuName = "LichLord/Maneuvers/GunManeuverDefinition", order = 2)]
    public class GunManeuverDefinition : ManeuverDefinition
    {
        [SerializeField] private GameObject gunModelPrefab; // Prefab for the gun model to spawn
        [SerializeField] private ProjectileDefinition projectileDefinition; // For ProjectileManager

        private GameObject spawnedGunModel; // Runtime reference to the spawned gun

        public override void SelectAction(PlayerCreature playerCreature, NetworkRunner runner)
        {
        }

        public override void DeselectAction(PlayerCreature playerCreature, NetworkRunner runner)
        {
        }

        public override void Execute(PlayerCreature playerCreature, NetworkRunner runner)
        {
            // Get the action spawn point
            Transform spawnPoint = playerCreature.Actions.ActionSpawnPoint != null ? playerCreature.Actions.ActionSpawnPoint : playerCreature.transform;
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
                    Debug.LogWarning($"[GunActionData] Cannot fire projectile for {ActionName}: ProjectileManager not found.");
                }
            }


            // Trigger RPC via CreatureActions
            if (playerCreature.Actions != null)
            {
                playerCreature.Actions.RPC_ExecuteGunAction(playerCreature.Object.Id, TableID, spawnPoint.position, targetPos, hitPosition, hitNormal);
            }
            
        }
    }
}