using DWD.Utility.Loading;
using LichLord.Buildables;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIBuildableSlotSection : UIWidget
    {
        [SerializeField]
        private List<UIBuildableSlot> _slots = new List<UIBuildableSlot> ();

        [SerializeField]
        private UIBuildableSlot _deleteSlot;

        private EBuildableCategory _buildableCategory = EBuildableCategory.None;

        protected override void OnTick()
        {
            base.OnTick();

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            if (pc.Builder.BuildableCategory != _buildableCategory)
            {
                RebuildIcons();
                _buildableCategory = pc.Builder.BuildableCategory;
            }
        }

        protected override void OnEnable()
        {
            // Rebuild icons
            RebuildIcons();
        }

        public void RebuildIcons()
        {
            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return;

            var builder = pc.Builder;
            var activeBuildables = builder.ActiveBuildables;

            int buildableCount = activeBuildables?.Count ?? 0;

            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < buildableCount)
                {
                    // reuse this slot and activate
                    _slots[i].gameObject.SetActive(true);
                }
                else
                {
                    // beyond available buildables, hide/collapse
                    _slots[i].gameObject.SetActive(false);
                }
            }

            _deleteSlot.gameObject.SetActive(true);
        }
    }
}
