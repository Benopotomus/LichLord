using DG.Tweening;
using DWD.Pooling;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableStateComponent : MonoBehaviour
    {
        [SerializeField] private Buildable _buildable;
        public Buildable Buildable => _buildable;

        [SerializeField] private EBuildableState _currentState = EBuildableState.Inactive;
        public EBuildableState CurrentState => _currentState;

        [SerializeField]
        private VisualEffectBase _hitReactPrefab;

        [SerializeField]
        private float _shakeDuration = 0.25f;

        [SerializeField]
        private float _shakePositionStrength = 0.05f;

        [SerializeField]
        private float _shakeRotationStrength = 1.0f;

        public void UpdateState(EBuildableState newState)
        {
            if (_currentState == newState)
                return;

            switch (newState)
            {
                case EBuildableState.Inactive:
                case EBuildableState.Destroyed:
                    //gameObject.SetActive(false);
                    break;
                case EBuildableState.Idle:
                    //gameObject.SetActive(true);
                    break;
                case EBuildableState.HitReact:
                    PlayHitReactShake();
                    if (_hitReactPrefab != null)
                    {
                        var effectInstance = DWDObjectPool.Instance.SpawnAt(_hitReactPrefab,
                            _buildable.CachedTransform.position, _buildable.CachedTransform.rotation) as VisualEffectBase;
                        effectInstance.Initialize();
                    }

                    break;
            }

            _currentState = newState;
        }

        public void PlayHitReactShake()
        {
            if (_shakeDuration <= 0f)
                return;

            // Create a DOTween Sequence to handle both shake effects
            
            Sequence shakeSequence = DOTween.Sequence();

            if (_shakePositionStrength > 0f)
            {
                // Add position shake
                shakeSequence.Join(transform.DOShakePosition(
                    duration: _shakeDuration, // Duration of the shake
                    strength: _shakePositionStrength, // Strength of the position shake
                    vibrato: 20, // Number of oscillations
                    randomness: 90, // Randomness of the shake
                    snapping: false, // Smooth movement
                    fadeOut: true // Gradually reduce shake intensity
                ));
            }

            if (_shakeRotationStrength > 0f)
            {
                // Add rotation shake
                shakeSequence.Join(transform.DOShakeRotation(
                    duration: _shakeDuration, // Same duration as position shake
                    strength: new Vector3(_shakeRotationStrength, _shakeRotationStrength, _shakeRotationStrength), // Slight rotation shake (degrees)
                    vibrato: 20, // Number of oscillations
                    randomness: 90, // Randomness of the shake
                    fadeOut: true // Gradually reduce shake intensity
                ));
            }
        }
    }
}
