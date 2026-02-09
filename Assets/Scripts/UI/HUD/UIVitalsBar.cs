using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord
{
    public class UIVitalsBar : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _rectTransform;

        [SerializeField]
        private Slider _vitalSlider;

        [SerializeField]
        private Slider _vitalLagSlider;

        [SerializeField]
        private float _vitalLagDelay = 0.2f;

        [SerializeField]
        private float _vitalLagDuration = 0.6f;

        [SerializeField]
        private float _pointsPerUnit = 0.6f;

        private Coroutine _vitalLagCoroutine;

        public void SetVitalMax(float max)
        {
            // Keep existing height, change only width
            float newWidth = max * _pointsPerUnit;

            _rectTransform.sizeDelta = new Vector2(
                newWidth,
                _rectTransform.sizeDelta.y 
            );
        }

        public void SetVitalPercent(float vitalPercent)
        { 
            var currentPercent = vitalPercent;

            _vitalSlider.value = currentPercent;

            if (currentPercent >= _vitalLagSlider.value)
            {
                _vitalLagSlider.value = currentPercent;
                if (_vitalLagCoroutine != null)
                {
                    StopCoroutine(_vitalLagCoroutine);
                    _vitalLagCoroutine = null;
                }
            }
            else
            {
                if (_vitalLagCoroutine != null)
                {
                    StopCoroutine(_vitalLagCoroutine);
                }
                _vitalLagCoroutine = StartCoroutine(StartVitalLag());
            }
        }

        private IEnumerator StartVitalLag()
        {
            yield return new WaitForSeconds(_vitalLagDelay);

            float startValue = _vitalLagSlider.value;
            float targetValue = _vitalSlider.value;
            float duration = _vitalLagDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                _vitalLagSlider.value = Mathf.Lerp(startValue, targetValue, t);

                yield return null;  // wait next frame
            }

            _vitalLagSlider.value = targetValue;
            _vitalLagCoroutine = null;
        }
    }
}
