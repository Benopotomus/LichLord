using System;
using UnityEngine;
using DG.Tweening;

namespace LichLord
{
    public class VisualEffectTransformer : MonoBehaviour
    {
        [SerializeField] private VisualEffectBase _visualEffectBase;
        [SerializeField] private Transform _transform;

        [SerializeField] private float _scaleDuration = 0.5f; // duration of the scale animation
        [SerializeField] private Ease _easeType = Ease.OutBack; // nice "pop" effect

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

        private void OnInitialized(VisualEffectBase gameplayEffectVisual)
        {
            if (_transform != null)
            {
                // Start from zero scale
                _transform.localScale = Vector3.zero;

                // Animate to full scale (1,1,1)
                _transform.DOScale(Vector3.one, _scaleDuration)
                          .SetEase(_easeType);
            }
        }

        private void OnToggled(VisualEffectBase effect, bool isOn)
        {
            // Optional: scale up/down on toggle
            if (_transform == null) return;

            if (isOn)
            {
                _transform.DOScale(Vector3.one, _scaleDuration).SetEase(_easeType);
            }
            else
            {
                _transform.DOScale(Vector3.zero, _scaleDuration).SetEase(_easeType);
            }
        }

        private void OnRecycled(VisualEffectBase gameplayEffectVisual)
        {
            // Reset scale if needed
            if (_transform != null)
                _transform.localScale = Vector3.zero;
        }

        private void OnRecycleDelayStart(VisualEffectBase obj)
        {
            // Optional: do something before recycling
        }

        private void OnDestroy()
        {
            if (_visualEffectBase != null)
            {
                _visualEffectBase.onInitialized -= OnInitialized;
                _visualEffectBase.onRecycleDelayStart -= OnRecycleDelayStart;
                _visualEffectBase.onRecycled -= OnRecycled;
                _visualEffectBase.onToggled -= OnToggled;
            }
        }
    }
}
