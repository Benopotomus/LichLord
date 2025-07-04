using DWD.Utility.Loading;
using LichLord.Buildables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIBuildableSlot : UIWidget
    {
        [SerializeField]
        private int _slot;

        [SerializeField]
        private TextMeshProUGUI _text;

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private Color _unselectedColor;

        [SerializeField]
        private Color _selectedColor;

        [SerializeField]
        private Color _activeColor;

        private BuildableDefinition _definition;

        private IconLoader _iconLoader = new IconLoader();

        protected override void OnTick()
        {
            base.OnTick();

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            _text.text = _slot.ToString();

            BuildableDefinition slotDefinition = pc.Builder.AvailableBuildables[_slot - 1];

            // Check if the definitin has changed. Load icon if it has
            if (_definition == null || slotDefinition.TableID != _definition.TableID)
            {
                LoadDefinition(slotDefinition);
            }

            BuildableDefinition selectedDefinition = pc.Builder.GetSelectedBuildable();
            if (selectedDefinition.TableID == _definition.TableID)
            {
                _iconImage.color = _selectedColor;
            }
            else
            {
                _iconImage.color = _unselectedColor;
            }
        }

        private void LoadDefinition(BuildableDefinition definition)
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
