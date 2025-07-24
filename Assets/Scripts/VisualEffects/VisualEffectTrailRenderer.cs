using UnityEngine;

namespace LichLord
{
    public class VisualEffectTrailRenderer : MonoBehaviour
    {
        [SerializeField] VisualEffectBase _visualEffectBase;
        [SerializeField] TrailRenderer[] _trailRenderers;

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
                foreach (var trailRenderer in _trailRenderers)
                {
                    trailRenderer.Clear();
                    trailRenderer.emitting = true;
                }
            }
            else
            {
                foreach (var trailRenderer in _trailRenderers)
                {
                    //trailRenderer.Clear();
                    trailRenderer.emitting = false;
                }
            }
        }

        private void OnInitialized(VisualEffectBase gameplayEffectVisual)
        {
            foreach (var trailRenderer in _trailRenderers)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
            }
        }

        private void OnRecycleDelayStart(VisualEffectBase obj)
        {
            foreach (var trailRenderer in _trailRenderers)
            {
                if (_clearSystemOnRecycleStart)
                    trailRenderer.Clear();
                
                    trailRenderer.emitting = false;
            }
        }

        private void OnRecycled(VisualEffectBase gameplayEffectVisual)
        {
            foreach (var trailRenderer in _trailRenderers)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = false;
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
