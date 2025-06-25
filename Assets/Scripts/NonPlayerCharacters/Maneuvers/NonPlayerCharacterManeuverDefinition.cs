using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "LichLord/Maneuvers/NPCManeuverDefinition", order = 1)]
    public class NonPlayerCharacterManeuverDefinition : ScriptableObject
    {
        [SerializeField]
        private string ActionName;

        [SerializeField]
        private int Damage = 10;

        [SerializeField]
        private int _cooldownTicks = 32;
        public int CooldownTicks => _cooldownTicks;

        [SerializeField]
        private float MovementStopRange = 2.5f;

        [SerializeField]
        private float AttackRange = 3f;

        [SerializeField]
        private int _stateTicks = 32;
        public int StateTicks => _stateTicks;

        [SerializeField]
        private float FaceTargetRange = 5f;

        [SerializeField]
        private bool _requiresEnemyTarget;
        public bool RequiresEnemyTarget => _requiresEnemyTarget;

        public float MovementStopRangeSqrt => MovementStopRange * MovementStopRange;
        public float FaceTargetRangeSqrt => FaceTargetRange * FaceTargetRange;

        [Header("Animations")]
        [SerializeField]
        private List<FAnimationTrigger> _animationTriggers = new List<FAnimationTrigger>();
        public List<FAnimationTrigger> AnimationTriggers => _animationTriggers;

        List<FManeuverProjectile> maneuverProjectiles = new List<FManeuverProjectile>();
    }

}
