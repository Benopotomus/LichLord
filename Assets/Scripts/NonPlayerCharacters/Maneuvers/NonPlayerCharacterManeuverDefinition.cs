using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "LichLord/Maneuvers/NPCManeuverDefinition", order = 1)]
    public class NonPlayerCharacterManeuverDefinition : ScriptableObject
    {
        public string ActionName;
        public int Damage = 10;
        public float Cooldown = 1f;
        public float MovementStopRange = 3f;
        public float StateTime = 1f;

        public bool RequiresEnemyTarget;

        [Header("Animations")]
        [SerializeField]
        private List<FAnimationTrigger> _animationTriggers = new List<FAnimationTrigger>();
        public List<FAnimationTrigger> AnimationTriggers => _animationTriggers;
    }

    [Serializable]
    public struct FAnimationTrigger
    {
        public int Action;
        public int Weapon;
        public int TriggerNumber;
        public bool IsMoving;
        public bool IsBlocking;
    }
}
