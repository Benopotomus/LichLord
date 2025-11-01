using LichLord.Buildables;
using LichLord.Props;
using System;
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

        [Header("Valid Target Types")]
        [SerializeField]
        private EManeuverTarget[] _validTargetTypes = new EManeuverTarget[0];
        public EManeuverTarget[] ValidTargetTypes => _validTargetTypes;

        // Precomputed bitmask (32-bit safe)
        [NonSerialized] private int _validTargetMask = 0;

        [Header("Projectiles")]
        [SerializeField]
        private List<FManeuverProjectile> _maneuverProjectiles = new List<FManeuverProjectile>();
        public List<FManeuverProjectile> ManeuverProjectiles => _maneuverProjectiles;

        // Called in Editor + Runtime when ScriptableObject is loaded
        private void OnEnable()
        {
            RebuildTargetMask();
        }

        private void RebuildTargetMask()
        {
            _validTargetMask = 0;
            foreach (var target in _validTargetTypes)
            {
                int index = (int)target;
                if (index >= 0 && index < 32)
                    _validTargetMask |= (1 << index);
            }
        }

        private bool IsTargetTypeValid(EManeuverTarget type)
        {
            int index = (int)type;
            return index >= 0 && index < 32 && (_validTargetMask & (1 << index)) != 0;
        }

        public override bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            if (brainComponent.AttackTarget == null) return false;
            if (brainComponent.NPC.RuntimeState.GetAttitude() != EAttitude.Hostile) return false;
            if (brainComponent.NPC.RuntimeState.GetCarriedItem().IsValid()) return false;

            //float distance = brainComponent.DistanceToAttackTarget;
            //if (distance < ValidTargetDistance.x || distance > ValidTargetDistance.y) return false;

            return brainComponent.AttackTarget switch
            {
                NonPlayerCharacter => IsTargetTypeValid(EManeuverTarget.NPC),
                PlayerCharacter => IsTargetTypeValid(EManeuverTarget.PC),
                Stronghold => IsTargetTypeValid(EManeuverTarget.Stronghold),
                Prop => IsTargetTypeValid(EManeuverTarget.Prop),
                Buildable => IsTargetTypeValid(EManeuverTarget.Buildable),
                _ => false
            };
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
