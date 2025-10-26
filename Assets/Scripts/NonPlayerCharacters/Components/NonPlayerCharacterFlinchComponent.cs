using DG.Tweening;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterFlinchComponent : MonoBehaviour
    {
        [SerializeField] private Transform[] _shakeBones;

        private float _flinchTime = 0.35f;
        private float _shakeRotationStrength = 4f;
        private int _vibrato = 18;
        private float _randomness = 90f;

        public void TriggerFlinch()
        {
            foreach (Transform bone in _shakeBones)
            {
                // Kill any previous shake on this bone
                bone.DOKill();

                // Slightly randomize shake strength per bone
                float randomStrengthX = _shakeRotationStrength * Random.Range(0.8f, 1.2f);
                float randomStrengthY = _shakeRotationStrength * Random.Range(0.8f, 1.2f);
                float randomStrengthZ = _shakeRotationStrength * Random.Range(0.8f, 1.2f);

                // Start the shake
                bone.DOShakeRotation(
                        duration: _flinchTime,
                        strength: new Vector3(randomStrengthX, randomStrengthY, randomStrengthZ),
                        vibrato: _vibrato,
                        randomness: _randomness,
                        fadeOut: true
                    )
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(UpdateType.Late)
                    .OnComplete(() =>
                    {
                        // Smoothly return to rest afterward
                        bone.DOLocalRotate(Vector3.zero, 0.05f, RotateMode.LocalAxisAdd)
                            .SetEase(Ease.OutSine)
                            .SetUpdate(UpdateType.Late);
                    });
            }
        }
    }
}
