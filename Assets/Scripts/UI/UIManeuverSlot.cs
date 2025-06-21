using DWD.Utility.Loading;
using System;
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

        private ManeuverDefinition _definition;

        private AssetBundleLoader IconLoader;

        protected override void OnTick()
        {
            base.OnTick();

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            _text.text = _slot.ToString();

            ManeuverDefinition definition = pc.Maneuvers.AvailableManeuvers[_slot - 1];

            // Check if the definitin has changed. Load icon if it has
            if (_definition == null || definition.TableID != _definition.TableID)
            {
                LoadDefinition(definition);
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
            if (prefabBundle.Ready == false)
            {
                Debug.LogWarning("Cannot spawn Icon for " + _definition.ManeuverName + ". Bundle is not ready.");
                return;
            }

            IconLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            if (IconLoader != null)
            {
                if (IconLoader.IsLoaded)
                    SpawnLoadedIcon(IconLoader);
                else
                    IconLoader.OnLoadComplete += OnVisualsPrefabLoaded;
            }
        }

        private void OnVisualsPrefabLoaded(ILoader clipLoader)
        {
            if (IconLoader != null)
                IconLoader.OnLoadComplete -= OnVisualsPrefabLoaded;

            SpawnLoadedIcon(clipLoader);
        }

        private void SpawnLoadedIcon(ILoader clipLoader)
        {
            AssetBundleLoader loader = clipLoader as AssetBundleLoader;
            Sprite sprite = loader.GetAssetWithin<Sprite>();

            if (_definition == null)
            {
                return;
            }

            _iconImage.sprite = sprite;
        }
    }
}
