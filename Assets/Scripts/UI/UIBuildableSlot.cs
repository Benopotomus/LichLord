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

        protected override void OnVisible()
        {
            _text.text = _slot.ToString();
        }

        protected override void OnTick()
        {
            base.OnTick();

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            if (_slot == 0)
            {
                if (pc.Builder.IsDeleteMode)
                {
                    _iconImage.color = _activeColor;
                }
                else
                {
                    _iconImage.color = _unselectedColor;
                }
                _text.text = "X"; // label for delete
                return;
            }

            int index = _slot - 1;

            var activeBuildables = pc.Builder.ActiveBuildables;

            if (activeBuildables == null || index >= activeBuildables.Count)
                return;

            BuildableDefinition slotDefinition = activeBuildables[index];

            if (slotDefinition == null)
                return;

            // Check if the definitin has changed. Load icon if it has
            if (_definition == null || 
                slotDefinition.TableID != _definition.TableID)
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
