using LichLord.Projectiles;
using UnityEngine;

// This component binds to a projectile's impact event and toggles on
// particles and off others

namespace LichLord
{
    public class VisualEffectImpactParticleSystem : MonoBehaviour
    {
        [SerializeField] ProjectileVisualEffect _projectileVisualEffect;
        [SerializeField] ParticleSystem[] _particleSystems;

        [SerializeField] bool _clearSystemOnRecycleStart;

        [SerializeField] ParticleSystem[] _disableSystems;

        private void Awake()
        {
            if (_projectileVisualEffect != null)
            {
                _projectileVisualEffect.onRecycleDelayStart += OnRecycleDelayStart;
                _projectileVisualEffect.onRecycled += OnRecycled;
                _projectileVisualEffect.onImpacted += OnImpacted;
            }
        }

        private void OnImpacted(VisualEffectBase gameplayEffectVisual)
        {
            foreach (var particle in _particleSystems)
            {
                particle.Stop();
                particle.Clear();
                particle.Play();
            }

            foreach (var particle in _disableSystems)
            {
                particle.Stop();
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
            if (_projectileVisualEffect != null)
            {
                _projectileVisualEffect.onImpacted -= OnImpacted;
                _projectileVisualEffect.onRecycleDelayStart -= OnRecycleDelayStart;
                _projectileVisualEffect.onRecycled -= OnRecycled;
            }
        }
    }
}
