using DWD.Utility.Loading;
using LichLord.NonPlayerCharacters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UICommanderUnitIcon : UIWidget
    {
        [SerializeField] protected Image _iconImage;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TextMeshProUGUI _healthText;

        private NonPlayerCharacter _npc;
        protected NonPlayerCharacterDefinition _npcDefinition;
        private IconLoader _iconLoader = new IconLoader();

        public virtual void SetCommandUnit(FCommandUnit commandUnit)
        {
            if(!commandUnit.IsFilled)
            {
                //Hidden();
                _iconImage.enabled = false;
                _healthSlider.enabled = false;
                _healthSlider.gameObject.SetActive(false);
                _healthText.enabled = false;

                if(_npc != null) 
                    _npc.Health.OnHealthChanged -= SetHealth;

                return;
            }

            //Visible();

            _npc = commandUnit.NPC;

            _npcDefinition = _npc.RuntimeState.Definition;

            if (_npcDefinition != null)
            {
                LoadIcon(_npcDefinition.Icon);
                SetHealth(_npc.Health.CurrentHealth, _npc.Health.MaxHealth);
                _npc.Health.OnHealthChanged += SetHealth;
            }
        }

        public void SetHealth(int currentHealth, int maxHealth)
        {
            _healthSlider.gameObject.SetActive(true);
            _healthSlider.enabled = true;
            _healthText.enabled = true;
            _healthText.text = currentHealth + " / " + maxHealth;
            _healthSlider.value = (float)currentHealth / (float)maxHealth;
        }

        protected void LoadIcon(BundleObject prefabBundle)
        {
            _iconLoader.OnLoaded += OnIconLoaded;
            _iconLoader.LoadIcon(prefabBundle);
        }

        protected void OnIconLoaded(IconLoader iconLoader, Sprite sprite)
        {
            _iconLoader.OnLoaded -= OnIconLoaded;
            _iconImage.sprite = sprite;
            _iconImage.enabled = true;
        }
    }
}
