using LichLord.NonPlayerCharacters;
using UnityEngine;
using Fusion;

namespace LichLord.Projectiles
{
    [CreateAssetMenu(menuName = "LichLord/Projectiles/ProjectileImpactActionDefinition")]
    public class ProjectileImpactActionDefinition : ScriptableObject
    {
        [SerializeField]
        private NonPlayerCharacterDefinition[] _summonedCharacters = new NonPlayerCharacterDefinition[0];
        public NonPlayerCharacterDefinition[] SummonedCharacters => _summonedCharacters;

        [SerializeField] private LayerMask hitMask = ~0; // used to ground npcs on replication

        [SerializeField] private float raycastLength = 6f; // used to ground npcs on replication

        public void Trigger(ref FProjectileData data, ref FPhysicsHitData impactHit, FixedUpdateProjectile projectile)
        {
            PlayerCharacter pc = projectile.Instigator as PlayerCharacter;

            if (pc == null)
                return;

            int playerIndex = pc.PlayerIndex;

            var formationComponent = pc.Formation;
            if (formationComponent == null) 
                return;

            int freeFormationId = formationComponent.GetFreeFormationID();
            if (freeFormationId == -1)
                return;

            for (int i = 0; i < SummonedCharacters.Length; i++)
            {
                var character = SummonedCharacters[i];

                Vector3 randomPosition = new Vector3(
                Random.Range(-5f, 5f),
                0, // Keep Y fixed
                Random.Range(-5f, 5f)
                );

                // Combine offset into raycast origin
                if (Physics.Raycast((randomPosition + impactHit.ImpactPoint) + 
                    (Vector3.up * (raycastLength * 0.5f)), 
                    Vector3.down, 
                    out RaycastHit hit,
                    raycastLength, 
                    hitMask))
                {
                    projectile.Context.NonPlayerCharacterManager.RPC_SpawnNPCWarrior(hit.point,
                    (ushort)character.TableID,
                    ETeamID.PlayerTeam,
                    (byte)playerIndex,
                    (byte)freeFormationId,
                    (byte)i);
                }


            }
        }
    }
}
