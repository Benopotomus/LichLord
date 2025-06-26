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
        private int _damage = 10;
        public int Damage => _damage;

        [SerializeField]
        private int _cooldownTicks = 32;
        public int CooldownTicks => _cooldownTicks;

        [SerializeField]
        private float _movementStopRange = 2.5f;

        [SerializeField]
        private float _attackRange = 3f;
        public float AttackRange => _attackRange;

        [SerializeField]
        private int _stateTicks = 32;
        public int StateTicks => _stateTicks;

        [SerializeField]
        private float _faceTargetRange = 5f;

        [SerializeField]
        private bool _requiresEnemyTarget;
        public bool RequiresEnemyTarget => _requiresEnemyTarget;

        public float MovementStopRangeSqrt => _movementStopRange * _movementStopRange;
        public float FaceTargetRangeSqrt => _faceTargetRange * _faceTargetRange;

        [Header("Animations")]
        [SerializeField]
        private List<FAnimationTrigger> _animationTriggers = new List<FAnimationTrigger>();
        public List<FAnimationTrigger> AnimationTriggers => _animationTriggers;

        [Header("Projectiles")]
        [SerializeField]
        private List<FManeuverProjectile> _maneuverProjectiles = new List<FManeuverProjectile>();
        public List<FManeuverProjectile> ManeuverProjectiles => _maneuverProjectiles;
    }

}
