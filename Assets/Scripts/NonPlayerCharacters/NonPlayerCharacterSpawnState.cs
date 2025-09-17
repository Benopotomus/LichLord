using DWD.Utility.Loading;
using System;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public class NonPlayerCharacterSpawnState
    {
        public float StateTime = 2f;

        [SerializeField]
        private FAnimationTrigger _animationTrigger;
        public FAnimationTrigger AnimationTrigger => _animationTrigger;

        [BundleObject(typeof(GameObject))]
        [SerializeField]
        private BundleObject _spawnEffect;
        public BundleObject SpawnEffect => _spawnEffect;
    }
}
