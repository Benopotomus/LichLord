using System;
using UnityEngine;
using UnityEngine.VFX;

namespace LichLord
{
    public class VisualEffectGraphSystem : MonoBehaviour
    {
        [SerializeField] VisualEffectBase _visualEffectBase;
        [SerializeField] VisualEffect[] _particleSystems;

        [SerializeField] bool _clearSystemOnRecycleStart;

        private void Awake()
        {
            if (_visualEffectBase != null)
            {
                _visualEffectBase.onInitialized += OnInitialized;
                _visualEffectBase.onRecycleDelayStart += OnRecycleDelayStart;
                _visualEffectBase.onRecycled += OnRecycled;
                _visualEffectBase.onToggled += OnToggled;
            }
        }

        private bool _isOn;

        private void OnToggled(VisualEffectBase effect, bool isOn)
        {
            if (_isOn == isOn)
                return;

            _isOn = isOn;

            if (isOn)
            {
                foreach (var particle in _particleSystems)
                {
                    particle.Stop();
                    particle.Reinit();
                    particle.Play();
                }
            }
            else
            {
                foreach (var particle in _particleSystems)
                {
                    particle.Stop();
                    //particle.Clear();
                }
            }
        }

        private void OnInitialized(VisualEffectBase gameplayEffectVisual)
        {
            foreach (var particle in _particleSystems)
            {
                particle.enabled = true;
                particle.Stop();
                particle.Reinit();
                particle.Play();
            }
        }

        private void OnRecycleDelayStart(VisualEffectBase obj)
        {
            foreach (var particle in _particleSystems)
            {
                particle.Stop();
            }

            if (_clearSystemOnRecycleStart)
            {
                foreach (var particle in _particleSystems)
                    particle.Reinit();
            }
        }

        private void OnRecycled(VisualEffectBase gameplayEffectVisual)
        {
            foreach (var particle in _particleSystems)
            {
                particle.Stop();
                particle.Reinit();
                particle.enabled = false;
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
