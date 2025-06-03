/// A standalone visual effect is one that controls its own lifetime.
/// for things like blood and hit splats.

namespace LichLord
{
    using UnityEngine;
    using System.Collections;

    public class StandaloneVisualEffect : VisualEffectBase
    {
        [SerializeField] protected float _lifetime;

        private float _lifetimeRemaining;

        private Coroutine _coroutine;

        public override void Initialize()
        {
            _lifetimeRemaining = _lifetime;

            if (_lifetime > 0)
                _coroutine = StartCoroutine(UpdateLifetime());

            onInitialized?.Invoke(this);
        }

        private IEnumerator UpdateLifetime()
        {
            // Run the loop as long as the lifetime hasn't elapsed
            while (_lifetimeRemaining > 0)
            {
                _lifetimeRemaining -= Time.deltaTime;
                onUpdate?.Invoke(this);
                yield return null;  // Wait for the next frame
            }

            // When time is up, recycle the effect
            RecycleVisualEffect();
        }

        protected override void RecycleVisualEffect()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);  // Ensure all coroutines are stopped
                _coroutine = null;
            }

            base.RecycleVisualEffect();  // Call base recycling method
        }
    }
}
