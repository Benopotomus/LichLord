using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace LichLord.UI
{ 
    public class UILoadoutWidget : UIInventoryContextWidget
    {

        [SerializeField]
        [SerializedDictionary("Loadout Slot", "SlotWidget")]
        private SerializedDictionary<ELoadoutSlot, UIItemSlot> _loadoutSlots;

        protected override void OnTick()
        {
            base.OnTick();

            RefreshLoadoutItems();
        }

        public void RefreshLoadoutItems()
        {
            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
                return;

            PlayerInventoryComponent inventory = pc.Inventory;

            foreach (var kvp in _loadoutSlots)
            { 
                var itemData = inventory.GetItemAtLoadoutSlot(kvp.Key);
                kvp.Value.SetItemData(itemData);
            }
        }
    }
}
