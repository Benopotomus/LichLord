using DG.Tweening;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterFlinchComponent : MonoBehaviour
    {
        [SerializeField] private Transform[] _shakeBones;

        private float _flinchTime = 0.35f;
        private float _shakeRotationStrength = 2f;
        private int _vibrato = 15;
        private float _randomness = 90f;

        private float _shakePositonStrength = 0.1f;

        public void TriggerFlinch()
        {
            foreach (Transform bone in _shakeBones)
            {
                // Slightly randomize shake strength per bone
                float randomStrengthX = _shakeRotationStrength * Random.Range(0.8f, 1.2f);
                float randomStrengthY = _shakeRotationStrength * Random.Range(0.8f, 1.2f);
                float randomStrengthZ = _shakeRotationStrength * Random.Range(0.8f, 1.2f);

                // Create a DOTween Sequence to handle both shake effects
                Sequence shakeSequence = DOTween.Sequence();

                // Add position shake
                shakeSequence.Join(bone.DOShakePosition(
                    duration: _flinchTime, // Duration of the shake
                    strength: _shakePositonStrength, // Strength of the position shake
                    vibrato: _vibrato, // Number of oscillations
                    randomness: _randomness, // Randomness of the shake
                    snapping: false, // Smooth movement
                    fadeOut: true // Gradually reduce shake intensity
                ));

                shakeSequence.SetUpdate(UpdateType.Late);

                shakeSequence.OnComplete(() =>
                {

                });
            }
        }
    }
}
