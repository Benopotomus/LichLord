using DWD.Utility.Loading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIManeuverSlot : UIWidget
    {
        [SerializeField]
        private int _slot;

        [SerializeField]
        private TextMeshProUGUI _text;

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private Image _cooldownImage;

        [SerializeField]
        private Color _unselectedColor;

        [SerializeField]
        private Color _selectedColor;

        [SerializeField]
        private Color _activeColor;

        private ManeuverDefinition _definition;

        private IconLoader _iconLoader = new IconLoader();

        protected override void OnTick()
        {
            base.OnTick();

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            _text.text = _slot.ToString();

            if (_slot > pc.Maneuvers.SpellManeuvers.Count)
                return;

            ManeuverDefinition slotDefinition = pc.Maneuvers.SpellManeuvers[_slot - 1];

            // Check if the definitin has changed. Load icon if it has
            if (_definition == null || slotDefinition.TableID != _definition.TableID)
            {
                LoadDefinition(slotDefinition);
            }

            _cooldownImage.fillAmount = pc.Maneuvers.GetCooldownPercent(_slot-1);

            _iconImage.color = _unselectedColor;

            if (_definition == null)
                return;

            ManeuverDefinition selectedDefinition = pc.Maneuvers.GetSelectedManeuver();
            if (selectedDefinition.TableID == _definition.TableID)
            {
                _iconImage.color = _selectedColor;
            }

            ManeuverDefinition activeDefinition = pc.Maneuvers.GetActiveManeuver();
            if (activeDefinition == null)
                return;

            if (activeDefinition.TableID == _definition.TableID)
            {
                _iconImage.color = _activeColor;
            }
        }

        private void LoadDefinition(ManeuverDefinition definition)
        {
            _definition = definition;
            LoadIcon(_definition.Icon);
        }

        // VISUALS

        private void LoadIcon(BundleObject prefabBundle)
        {
            _iconLoader.OnLoaded += OnIconLoaded;
            _iconLoader.LoadIcon(prefabBundle);
        }

        private void OnIconLoaded(IconLoader iconLoader, Sprite sprite)
        {
            _iconLoader.OnLoaded -= OnIconLoaded;
            _iconImage.sprite = sprite;
        }
    }
}
