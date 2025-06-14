using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public class NonPlayerCharacterHitReactState
    {
        public float StateTime = 1f;

        [SerializeField]
        private FAnimationTrigger _animationTrigger;
        public FAnimationTrigger AnimationTrigger => _animationTrigger;
    }
}
