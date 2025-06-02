using UnityEngine;

namespace LichLord
{
    public class VisualEffectParticleSystem : MonoBehaviour
    {
        [SerializeField] VisualEffectBase _visualEffectBase;
        [SerializeField] ParticleSystem[] _particleSystems;

        [SerializeField] bool _clearSystemOnRecycleStart;

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
            foreach (var particle in _particleSystems)
            {
                particle.Stop();
                particle.Clear();
                particle.Play();
            }
        }

        private void OnRecycleDelayStart(VisualEffectBase obj)
        {
            foreach (var particle in _particleSystems)
                particle.Stop();

            if (_clearSystemOnRecycleStart)
            {
                foreach (var particle in _particleSystems)
                    particle.Clear();
            }
        }

        private void OnRecycled(VisualEffectBase gameplayEffectVisual)
        {
            foreach (var particle in _particleSystems)
            {
                particle.Stop();
                particle.Clear();
            }
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
