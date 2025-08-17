using System;
using UnityEngine;
using DG.Tweening;

namespace LichLord
{
    public class BuildableSpawnTransformer : MonoBehaviour
    {
        [SerializeField] private Transform _transform;

        [SerializeField] private float _scaleDuration = 0.5f; // duration of the scale animation
        [SerializeField] private Ease _easeType = Ease.OutBack; // nice "pop" effect

        public void PlaySpawnAnimation()
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

        public void PlayDestroyAnimation()
        {
            if (_transform != null)
            {
                // Start from zero scale
                _transform.localScale = Vector3.one;

                // Animate to full scale (1,1,1)
                _transform.DOScale(Vector3.zero, _scaleDuration)
                          .SetEase(Ease.InOutCubic);
            }
        }
    }
}
