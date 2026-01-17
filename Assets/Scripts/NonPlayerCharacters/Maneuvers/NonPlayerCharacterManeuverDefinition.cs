using LichLord.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "LichLord/Maneuvers/NPCManeuverDefinition", order = 1)]
    public class NonPlayerCharacterManeuverDefinition : ScriptableObject
    {
        [SerializeField]
        protected string ActionName;

        public virtual EManeuverType ManeuverType => EManeuverType.None;

        [SerializeField]
        private int _cooldownTicks = 32;
        public int CooldownTicks => _cooldownTicks;

        [SerializeField]
        private float _movementStopRange = 2.5f;

        [SerializeField]
        private Vector2 _activationRange = new Vector2(0, 2.5f);

        [SerializeField]
        private float _attackRange = 3f;
        public float AttackRange => _attackRange;

        [SerializeField]
        private int _stateTicks = 32;
        public int StateTicks => _stateTicks;

        [SerializeField]
        private float _faceTargetRange = 5f;

        [SerializeField]
        private bool _requiresLOS = true;
        public bool RequiresLOS => _requiresLOS;

        public float MovementStopRangeSqrt => _movementStopRange * _movementStopRange;
        public float FaceTargetRangeSqrt => _faceTargetRange * _faceTargetRange;

        [SerializeField]
        private float _verticalAimOffset;
        public float VerticalAimOffset => _verticalAimOffset;

        [Header("Targeting")]
        [SerializeField]
        private Vector2 _validTargetDistance = Vector2.zero;
        public Vector2 ValidTargetDistance => _validTargetDistance;

        [Header("Animations")]
        [SerializeField]
        private List<FAnimationTrigger> _animationTriggers = new List<FAnimationTrigger>();
        public List<FAnimationTrigger> AnimationTriggers => _animationTriggers;

        [Header("Events")]
        [SerializeField]
        private List<NonPlayerCharacterManeuverHitEvent> _hitEvents = new List<NonPlayerCharacterManeuverHitEvent>();
        public List<NonPlayerCharacterManeuverHitEvent> HitEvents => _hitEvents;

        [SerializeField]
        private List<NonPlayerCharacterManeuverHitEvent> _specialEvents = new List<NonPlayerCharacterManeuverHitEvent>();
        public List<NonPlayerCharacterManeuverHitEvent> SpecialEvents => _specialEvents;

        public virtual bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            return false;
        }

        public void ExecuteHitEvents(NonPlayerCharacter npc, IChunkTrackable target)
        { 
            foreach(var hitEvent in HitEvents) 
                hitEvent.Execute(npc, this, target);
        }

        public void ExecuteSpecialEvents(NonPlayerCharacter npc, IChunkTrackable target)
        {
            foreach (var specialEvent in SpecialEvents)
                specialEvent.Execute(npc, this, target);
        }
    }

}
