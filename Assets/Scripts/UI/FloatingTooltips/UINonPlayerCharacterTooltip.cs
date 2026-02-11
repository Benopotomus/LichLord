using LichLord.NonPlayerCharacters;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace LichLord.UI
{
    public class UINonPlayerCharacterTooltip : UIFloatingWidget
    {
        [SerializeField] private NonPlayerCharacter _npc;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _damageText;
        [SerializeField] private Slider _healthbar;
        [SerializeField] private Slider _lifetimebar;           // drag in inspector

        [Header("Damage Popup Settings")]
        [SerializeField] private float damageShowDuration = 1.5f;
        [SerializeField] private float damageFadeDuration = 1.2f;
        [SerializeField] private Color damageColor = Color.red;

        private int _pendingDamage;
        private Coroutine _damageDisplayRoutine;

        public bool VisibleFromTracker;
        public bool VisibleFromDamage;

        public void UpdateVisibility()
        {
            if (VisibleFromDamage ||
                VisibleFromTracker)
            {
                Visible();
                return;
            }

            Hidden();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_damageText != null)
                _damageText.color = new Color(1, 1, 1, 0);     // fully transparent
        }

        public void SetNpcData(NonPlayerCharacter npc)
        {
            // Clean up previous NPC if any
            if (_npc != null)
            {
                _npc.Health.OnHealthChanged -= OnHealthChanged;
                if (_npc.RuntimeState.IsCommandedUnit())
                    _npc.Lifetime.OnLifetimeProgressChanged -= OnLifetimeProgressChanged;
            }

            _npc = npc;
            _pendingDamage = 0;

            if (_npc == null) return;

            OnHealthChanged(_npc.Health.CurrentHealth, _npc.Health.CurrentHealth, _npc.Health.MaxHealth);
            _npc.Health.OnHealthChanged += OnHealthChanged;

            bool isMinion = _npc.RuntimeState.IsCommandedUnit();
            if (_lifetimebar != null)
            {
                _lifetimebar.gameObject.SetActive(isMinion);

                if (isMinion)
                {
                    OnLifetimeProgressChanged(_npc.Lifetime.LifetimeProgress, _npc.Lifetime.LifetimeProgressMax);
                    _npc.Lifetime.OnLifetimeProgressChanged += OnLifetimeProgressChanged;
                }
            }
        }

        private void OnLifetimeProgressChanged(int current, int max)
        {
            if (max <= 0) return;
            _lifetimebar.value = (float)current / max;
        }

        private void OnHealthChanged(int oldHealth, int currentHealth, int maxHealth)
        {
            if (maxHealth <= 0) return;

            float newFill = (float)currentHealth / maxHealth;
            _healthbar.value = newFill;

            // Damage taken
            if (currentHealth < oldHealth)
            {
                int damage = oldHealth - currentHealth;
                _pendingDamage += damage;

                // (Re)start damage popup
                if (_damageDisplayRoutine != null)
                    StopCoroutine(_damageDisplayRoutine);

                _damageDisplayRoutine = StartCoroutine(ShowDamagePopup());
            }
            // Optional: handle healing differently if you want (green +number, etc.)
        }

        private IEnumerator ShowDamagePopup()
        {
            VisibleFromDamage = true;
            UpdateVisibility();
            // Show current accumulated damage
            _damageText.text = $"{_pendingDamage}";
            _damageText.color = damageColor;                      // visible red
            _damageText.color = new Color(damageColor.r, damageColor.g, damageColor.b, 1f);

            yield return new WaitForSeconds(damageShowDuration);

            // Fade out
            float elapsed = 0f;
            Color startColor = _damageText.color;

            while (elapsed < damageFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / damageFadeDuration;
                _damageText.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
                yield return null;
            }

            _damageText.color = new Color(1, 1, 1, 0); // ensure fully invisible
            _pendingDamage = 0;                      // reset counter
            VisibleFromDamage = false;
            UpdateVisibility();
            _damageDisplayRoutine = null;
        }

        protected override void OnHidden()
        {
            // Optional: stop any running damage animation
            if (_damageDisplayRoutine != null)
            {
                StopCoroutine(_damageDisplayRoutine);
                _damageDisplayRoutine = null;
            }

            if (_damageText != null)
                _damageText.color = new Color(1, 1, 1, 0);

            base.OnHidden();
        }

        protected override void OnDisable()
        {
            // Extra safety cleanup
            if (_damageDisplayRoutine != null)
            {
                StopCoroutine(_damageDisplayRoutine);
                _damageDisplayRoutine = null;
            }

            base.OnDisable();
        }
    }
}