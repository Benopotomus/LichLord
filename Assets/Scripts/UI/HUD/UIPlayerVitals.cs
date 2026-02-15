using UnityEngine.UI;
using UnityEngine;
using System.Collections;

namespace LichLord.UI
{
    public class UIPlayerVitals : UIWidget
    {
        [SerializeField]
        private UIVitalsBar _healthBar;

        [SerializeField]
        private UIVitalsBar _manaBar;

        private PlayerCharacter _pc;

        protected override void OnVisible()
        {
            base.OnVisible();

            StartCoroutine(BindPlayerCharacter());
        }

        protected override void OnHidden()
        {
            if (_pc != null)
            {
                _pc.Stats.OnStatChanged -= OnStatChanged;
            }

            base.OnHidden();
        }

        private IEnumerator BindPlayerCharacter()
        {
            while (Context.LocalPlayerCharacter == null)
            {
                yield return null;  // Wait one frame and check again
            }

            _pc = Context.LocalPlayerCharacter;
            OnStatChanged(EStatName.HealthMax);
            OnStatChanged(EStatName.ManaMax);

            _pc.Stats.OnStatChanged += OnStatChanged;
        }

        private void OnStatChanged(EStatName name)
        {
            switch (name)
            {
                case EStatName.HealthMax:
                case EStatName.HealthCurrent:
                    _healthBar.SetVitalMax(_pc.Stats.MaxHealth);
                    _healthBar.SetVitalPercent(_pc.Stats.HealthPercent);
                    break;

                case EStatName.ManaMax:
                case EStatName.ManaCurrent:
                    _manaBar.SetVitalMax(_pc.Stats.MaxMana);
                    _manaBar.SetVitalPercent(_pc.Stats.ManaPercent);
                    break;
            }
        }
    }
}
