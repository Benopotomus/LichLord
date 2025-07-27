using DWD.Utility.Loading;
using System;
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

        [BundleObject(typeof(GameObject))]
        [SerializeField]
        private BundleObject _hitEffect;
        public BundleObject HitEffect => _hitEffect;
    }
}
