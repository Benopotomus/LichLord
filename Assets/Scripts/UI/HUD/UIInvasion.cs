using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LichLord.UI
{
    public class UIInvasion : UIWidget
    {
        [SerializeField]
        private RectTransform _transform;

        [SerializeField]
        protected TextMeshProUGUI _text;

        [SerializeField]
        protected Image[] _iconImages;

        [SerializeField]
        protected Sprite _approachingSprite;

        [SerializeField]
        protected Sprite _retreatingSprite;

        private EInvasionState _lastInvasionState;

        protected override void OnTick()
        {
            InvasionManager invasionManager = Context.InvasionManager;

            if (invasionManager.InvasionID == 0)
            {
                _lastInvasionState = EInvasionState.None;
                return;
            }

            EInvasionState newState = invasionManager.InvasionState;

            if (newState != _lastInvasionState)
                OnStateChanged();

            _lastInvasionState = newState;

            switch (invasionManager.InvasionState)
            {
                case EInvasionState.Approaching:
                    _text.text = "SIEGE APPROACHING";
                    foreach (var image in _iconImages)
                        if (image != null)
                            image.sprite = _approachingSprite;
                    break;
                case EInvasionState.Retreating:
                    _text.text = "SIEGE RETREATING";
                    foreach (var image in _iconImages)
                        if (image != null)
                            image.sprite = _retreatingSprite;
                    break;
            }
        }

        private void OnStateChanged()
        {
            if (_transform == null)
            {
                Debug.LogWarning("UIInvasion: _transform is null, cannot animate");
                return;
            }

            // Stop any existing animations
            _transform.DOKill();

            // Bounce animation parameters
            float bounceScale = 1.2f; // Scale up to 120%
            float bounceDuration = 0.5f; // Duration per bounce (up + down)
            int bounceCount = 5; // Number of bounces

            // Create a sequence for bouncing
            Sequence bounceSequence = DOTween.Sequence();
            _transform.localScale = Vector3.one; // Reset scale

            // Define one full bounce (up and down)
            bounceSequence.Append(
                _transform.DOScale(bounceScale, bounceDuration / 2)
                    .SetEase(Ease.OutCubic)
            );
            bounceSequence.Append(
                _transform.DOScale(1f, bounceDuration / 2)
                    .SetEase(Ease.InQuad)
            );

            // Repeat for 5 full bounces
            bounceSequence.SetLoops(bounceCount, LoopType.Restart);
            bounceSequence.Play();

            //Debug.Log($"UIInvasion: Bounce animation started on _transform, {bounceCount} bounces, duration={bounceDuration}s per bounce");
        }
    }
}