
using NUnit.Framework.Constraints;
using System.Collections.Generic;

using UnityEngine;

namespace LichLord.UI
{
    public class UISummonSlotSection : UIWidget
    {
        [SerializeField]
        private List<UILoadoutSlot> _loadoutSlots;

        [SerializeField]
        private ELoadoutSlot _summonLoadoutSlot;

        private PlayerCharacter _pc;
        private SummonerComponent _summoner;

        protected override void OnVisible()
        {
            base.OnVisible();

            _pc = Context.LocalPlayerCharacter;
            if (_pc == null)
                return;

            _summoner = _pc.Summoner;

            OnSlotChanged(_summoner.SelectedSlot);
            _summonLoadoutSlot = _summoner.SelectedSlot;
        }

        protected override void OnTick()
        {
            base.OnTick();

            foreach (var slot in _loadoutSlots)
            {
                ELoadoutSlot loadoutSlotName = slot.LoadoutSlot;
                slot.SetItemData(_pc.Inventory.GetItemAtLoadoutSlot(loadoutSlotName));
            }

            if (_summonLoadoutSlot != _summoner.SelectedSlot)
            {
                OnSlotChanged(_summoner.SelectedSlot);
                _summonLoadoutSlot = _summoner.SelectedSlot;
            }
        }

        private void OnSlotChanged(ELoadoutSlot newSlot)
        {
            foreach (var slot in _loadoutSlots)
            {
                ELoadoutSlot loadoutSlotName = slot.LoadoutSlot;

                if (slot.LoadoutSlot == newSlot)
                {
                     slot.IconImage.color = new Color(1,1,1,1);
                }
                else
                {
                    slot.IconImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                }
            }
        }
    }
}
