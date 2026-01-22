using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "AdditiveHitReactionDefinition", menuName = "LichLord/HitReactions/AdditiveHitReactionDefinition")]
    public class AdditiveHitReactionDefinition : ScriptableObject
    {
        [SerializeField]
        private FAdditiveAnimationTrigger _additiveAnimationTrigger;
        public FAdditiveAnimationTrigger AdditiveAnimationTrigger => _additiveAnimationTrigger;

        [BundleObject(typeof(GameObject))]
        [SerializeField]
        private BundleObject _hitEffect;
        public BundleObject HitEffect => _hitEffect;

    }
}
