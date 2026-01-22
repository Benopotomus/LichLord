using LichLord.NonPlayerCharacters;
using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace LichLord.Projectiles
{
    [CreateAssetMenu(menuName = "LichLord/Projectiles/ProjectileImpactActionDefinition")]
    public class ProjectileImpactActionDefinition : ScriptableObject
    {
        [SerializeField]
        private NonPlayerCharacterDefinition[] _summonedCharacters = new NonPlayerCharacterDefinition[0];
        public NonPlayerCharacterDefinition[] SummonedCharacters => _summonedCharacters;


        public void Trigger(ref FProjectileData data, ref FPhysicsHitData impactHit, FixedUpdateProjectile projectile)
        {
            SpawnSummonedCharacters(ref data, ref impactHit, projectile);
        }

        public void SpawnSummonedCharacters(ref FProjectileData data, ref FPhysicsHitData impactHit, FixedUpdateProjectile projectile)
        {
            PlayerCharacter pc = projectile.Instigator as PlayerCharacter;

            if (pc == null)
                return;

            int playerIndex = pc.PlayerIndex;

            FWorldPosition hitPosition = new FWorldPosition();
            hitPosition.CopyPosition(impactHit.ImpactPoint);

            List<byte> validDefinitionIds = new List<byte>();

            for (int i = 0; i < _summonedCharacters.Length; i++)
            {
                if (_summonedCharacters[i] != null)
                {
                    validDefinitionIds.Add((byte)_summonedCharacters[i].TableID);
                }
            }

            projectile.Context.NonPlayerCharacterManager.RPC_SpawnCommandGroup(
                hitPosition,
                validDefinitionIds.ToArray(),
                ETeamID.PlayerTeam,
                (byte)playerIndex);

        }
    }
}
