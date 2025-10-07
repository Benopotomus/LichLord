using DG.Tweening;
using LichLord.Props;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIFloatingStrongholdStatus : UIFloatingWidget
    {
        [Header("UI Elements")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TextMeshProUGUI _healthText;

        [SerializeField] private Image _invasionIcon;

        private Stronghold _stronghold;

        private bool _invasionActive;

        private float bounceScale = 1.5f;
        private float duration = 0.35f;
        private Ease ease = Ease.OutQuad;

        protected override void OnTick()
        {
            base.OnTick();

            if (_stronghold != null)
            {
                _iconImage.SetActive(true);
                _healthSlider.SetActive(true);
                _healthText.SetActive(true);
                _healthText.text = _stronghold.CurrentHealth + " / " + _stronghold.MaxHealth;
                _healthSlider.value = (float)_stronghold.CurrentHealth / (float)_stronghold.MaxHealth;
            }
            else
            {
                _iconImage.SetActive(false);
                _healthSlider.SetActive(false);
                _healthText.SetActive(false);
            }

            if (Context.InvasionManager.InvasionID > 0 &&
                Context.InvasionManager.TargetStronghold == _stronghold)
            {
                if (!_invasionActive)
                {
                    _invasionIcon.SetActive(true);
                    _invasionActive = true;
                    Bounce();
                }
            }
            else
            {
                if (_invasionActive)
                {
                    _invasionIcon.SetActive(false);
                    _invasionActive = false;
                }
            }
        }

        public void SetStronghold(Stronghold stronghold)
        {
            _stronghold = stronghold;
        }

        public void Bounce()
        {
            _invasionIcon.transform.localScale = Vector3.one;

            _invasionIcon.transform.DOScale(Vector3.one * bounceScale, duration)
                  .SetEase(ease)
                  .SetLoops(64, LoopType.Yoyo);
        }

    }
}
