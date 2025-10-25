using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "HitReactionDefinition", menuName = "LichLord/HitReactions/HitReactionDefinition")]
    public class HitReactionDefinition : ScriptableObject
    {
        [SerializeField]
        private bool _isAdditive;
        public bool IsAdditive => _isAdditive;

        [SerializeField]
        private int _tickDuration;
        public int TickDuration => _tickDuration;

        [SerializeField]
        private FAnimationTrigger _animationTrigger;
        public FAnimationTrigger AnimationTrigger => _animationTrigger;

        [BundleObject(typeof(GameObject))]
        [SerializeField]
        private BundleObject _hitEffect;
        public BundleObject HitEffect => _hitEffect;

    }
}
