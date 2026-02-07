using LichLord.NonPlayerCharacters;
using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace LichLord.Projectiles
{
    [CreateAssetMenu(menuName = "LichLord/Projectiles/SummonProjectileImpactActionDefinition")]
    public class SummonProjectileImpactActionDefinition : ProjectileImpactActionDefinition
    {
        [SerializeField]
        private NonPlayerCharacterDefinition[] _summonedCharacters = new NonPlayerCharacterDefinition[0];
        public NonPlayerCharacterDefinition[] SummonedCharacters => _summonedCharacters;

        public override void Trigger(ref FProjectileData data, Projectile projectile)
        {
            base.Trigger(ref data, projectile);
            SpawnSummonedCharacters(ref data, projectile);
        }

        public void SpawnSummonedCharacters(ref FProjectileData data, Projectile projectile)
        {
            PlayerCharacter pc = projectile.Instigator as PlayerCharacter;

            if (pc == null)
                return;

            int playerIndex = pc.PlayerIndex;

            FWorldPosition hitPosition = new FWorldPosition();
            hitPosition.CopyPosition(data.TargetPosition);

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
