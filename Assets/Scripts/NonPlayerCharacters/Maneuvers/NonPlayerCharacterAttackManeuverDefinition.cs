using LichLord.Buildables;
using LichLord.Props;
using LichLord.World;
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
            if (!brainComponent.AttackTarget.HasTarget)
                return false;

            if(brainComponent.NPC.RuntimeState.GetAttitude() != EAttitude.Hostile)
                return false;

            var carriedItem = brainComponent.NPC.RuntimeState.GetCarriedItem();
            if (carriedItem.IsValid())
                return false;

            IChunkTrackable target = brainComponent.AttackTarget.Target;

            float distanceToTarget = Vector3.Distance(
            target.Position,
            brainComponent.NPC.Position);

            if (distanceToTarget < ValidTargetDistance.x ||
                distanceToTarget > ValidTargetDistance.y)
                return false;

            if (target is NonPlayerCharacter)
            {
                if (ValidTargetTypes.Contains(EManeuverTarget.NPC))
                    return true;
            }
            else if (target is PlayerCharacter)
            {
                if (ValidTargetTypes.Contains(EManeuverTarget.PC))
                    return true;
            }
            else if (target is Lair)
            {
                if (ValidTargetTypes.Contains(EManeuverTarget.Stronghold))
                    return true;
            }
            else if (target is Prop)
            {
                if (ValidTargetTypes.Contains(EManeuverTarget.Prop))
                    return true;
            }
            else if (target is Buildable)
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
