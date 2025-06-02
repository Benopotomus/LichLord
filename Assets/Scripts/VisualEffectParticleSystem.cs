using UnityEngine;

namespace LichLord
{
    [RequireComponent(typeof(ParticleSystem))]
    public class VisualEffectParticleSystem : MonoBehaviour
    {
        [SerializeField] VisualEffectBase _visualEffectBase;
        [SerializeField] ParticleSystem _particleSystem;

        private void Awake()
        {
            if (_visualEffectBase != null)
            {
                _visualEffectBase.onInitialized += OnInitialized;
                _visualEffectBase.onRecycleDelayStart += OnRecycleDelayStart;
                _visualEffectBase.onRecycled += OnRecycled;
            }
        }

        private void OnInitialized(VisualEffectBase gameplayEffectVisual)
        {
            _particleSystem.Stop();
            _particleSystem.Clear();
            _particleSystem.Play();
        }

        private void OnRecycleDelayStart(VisualEffectBase obj)
        {
            _particleSystem.Stop();
        }

        private void OnRecycled(VisualEffectBase gameplayEffectVisual)
        {
            if (_particleSystem == null)
                return;
            
            _particleSystem.Stop();
            _particleSystem.Clear();
        }

        private void OnDestroy()
        {
            if (_visualEffectBase != null)
            {
                _visualEffectBase.onInitialized -= OnInitialized;
                _visualEffectBase.onRecycleDelayStart -= OnRecycleDelayStart;
                _visualEffectBase.onRecycled -= OnRecycled;
            }
        }
    }
}
