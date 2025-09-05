using LichLord.Buildables;
using LichLord.Props;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "LichLord/Maneuvers/NPCAttackManeuverDefinition", order = 1)]
    public class NonPlayerCharacterAttackManeuverDefinition : NonPlayerCharacterManeuverDefinition
    {
        public override EManeuverType ManeuverType => EManeuverType.Attack;

        [SerializeField]
        protected int _damage = 10;
        public int Damage => _damage;

        [SerializeField]
        private List<EManeuverTarget> _validTargetTypes = new List<EManeuverTarget>();
        public List<EManeuverTarget> ValidTargetTypes => _validTargetTypes;

        [Header("Projectiles")]
        [SerializeField]
        private List<FManeuverProjectile> _maneuverProjectiles = new List<FManeuverProjectile>();
        public List<FManeuverProjectile> ManeuverProjectiles => _maneuverProjectiles;

        public override bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            if (brainComponent.AttackTarget == null)
                return false;

            if(brainComponent.NPC.RuntimeState.GetAttitude() != EAttitude.Hostile)
                return false;

            var carriedCurrency = brainComponent.NPC.RuntimeState.GetCarriedCurrencyType();
            if (carriedCurrency != ECurrencyType.None)
                return false;

            float distanceToTarget = Vector3.Distance(
            brainComponent.AttackTarget.Position,
            brainComponent.NPC.CachedTransform.position);

            if (distanceToTarget < ValidTargetDistance.x ||
                distanceToTarget > ValidTargetDistance.y)
                return false;

            if (brainComponent.AttackTarget is NonPlayerCharacter)
            {
                if (ValidTargetTypes.Contains(EManeuverTarget.NPC))
                    return true;
            }
            else if (brainComponent.AttackTarget is PlayerCharacter)
            {
                if (ValidTargetTypes.Contains(EManeuverTarget.PC))
                    return true;
            }
            else if (brainComponent.AttackTarget is Stronghold)
            {
                if (ValidTargetTypes.Contains(EManeuverTarget.Stronghold))
                    return true;
            }
            else if (brainComponent.AttackTarget is Prop)
            {
                if (ValidTargetTypes.Contains(EManeuverTarget.Prop))
                    return true;
            }
            else if (brainComponent.AttackTarget is Buildable)
            {
                if (ValidTargetTypes.Contains(EManeuverTarget.Buildable))
                    return true;
            }

            return false;
        }

    }

    public enum EManeuverTarget
    { 
        None,
        NPC,
        PC,
        Stronghold,
        Prop,
        Buildable,
        HarvestNode,
    }

    public enum EManeuverType
    {
        None,
        Attack,
        Harvest,
        Deposit,
    }
}
