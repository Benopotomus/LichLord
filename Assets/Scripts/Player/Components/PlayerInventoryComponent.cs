using LichLord.Items;
using LichLord.NonPlayerCharacters;
using UnityEngine;

namespace LichLord
{
    public class PlayerInventoryComponent : ContextBehaviour
    {
        private FItemData _weapon_00_left;
        private FItemData _weapon_00_right;
        private FItemData _weapon_01_left;
        private FItemData _weapon_01_right;
        private FItemData _weapon_02_left;
        private FItemData _weapon_02_right;

        private FItemData _summon_00;
        private FItemData _summon_01;
        private FItemData _summon_02;
        private FItemData _summon_03;
        private FItemData _summon_04;

        private FItemData[] _inventoryItems;

        private FItemData[] _squad_0;
        private FItemData[] _squad_1;
        private FItemData[] _squad_2;

        private int _carryWeight;
        public int CarryWeight => _carryWeight;

        private int _maxCarryWeight;
        public int MaxCarryWeight => _maxCarryWeight;

        public override void Spawned()
        {
            base.Spawned();

            TestLoadout testLoadout = Global.Settings.TestLoadout;
            _inventoryItems = testLoadout.CopyInventory();

            _weapon_00_left = testLoadout.CopyLoadoutItem(ELoadoutSlot.Weapon_00_Left);
            _weapon_00_right = testLoadout.CopyLoadoutItem(ELoadoutSlot.Weapon_00_Right);
            _weapon_01_left = testLoadout.CopyLoadoutItem(ELoadoutSlot.Weapon_01_Left);
            _weapon_01_right = testLoadout.CopyLoadoutItem(ELoadoutSlot.Weapon_01_Right);
            _weapon_02_left = testLoadout.CopyLoadoutItem(ELoadoutSlot.Weapon_02_Left);
            _weapon_02_right = testLoadout.CopyLoadoutItem(ELoadoutSlot.Weapon_02_Right);

            _summon_00 = testLoadout.CopyLoadoutItem(ELoadoutSlot.Summon_00);
            _summon_01 = testLoadout.CopyLoadoutItem(ELoadoutSlot.Summon_01);
            _summon_02 = testLoadout.CopyLoadoutItem(ELoadoutSlot.Summon_02);
            _summon_03 = testLoadout.CopyLoadoutItem(ELoadoutSlot.Summon_03);
            _summon_04 = testLoadout.CopyLoadoutItem(ELoadoutSlot.Summon_04);

            _inventoryItems = testLoadout.CopyInventory();

            _squad_0 = testLoadout.CopySquad(0);
            _squad_1 = testLoadout.CopySquad(1);
            _squad_2 = testLoadout.CopySquad(2);
        }

        public FItemData[] GetSquadItemsAtIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return _squad_0;
                case 1:
                    return _squad_1;
                case 2:
                    return _squad_2;
            }

            return new FItemData[0];
        }


        public FItemData GetItemAtLoadoutSlot(ELoadoutSlot slot)
        {
            switch (slot)
            {
                case ELoadoutSlot.Weapon_00_Left:
                    return _weapon_00_left;
                case ELoadoutSlot.Weapon_00_Right:
                    return _weapon_00_right;
                case ELoadoutSlot.Weapon_01_Left:
                    return _weapon_01_left;
                case ELoadoutSlot.Weapon_01_Right:
                    return _weapon_01_right;
                case ELoadoutSlot.Weapon_02_Left:
                    return _weapon_02_left;
                case ELoadoutSlot.Weapon_02_Right:
                    return _weapon_02_right;

                case ELoadoutSlot.Summon_00:
                    return _summon_00;
                case ELoadoutSlot.Summon_01:
                    return _summon_01;
                case ELoadoutSlot.Summon_02:
                    return _summon_02;
                case ELoadoutSlot.Summon_03:
                    return _summon_03;
                case ELoadoutSlot.Summon_04:
                    return _summon_04;
            }

            return new FItemData();
        }

        public FItemData GetItemAtInventorySlot(int slot)
        {
            if(slot >= _inventoryItems.Length)
                return new FItemData();

            return _inventoryItems[slot];
        }

        public void SetItemAtLoadoutSlot(ELoadoutSlot slot, in FItemData itemData)
        {
            switch (slot)
            {
                case ELoadoutSlot.Weapon_00_Left:
                    _weapon_00_left.Copy(itemData);
                    break;
                case ELoadoutSlot.Weapon_00_Right:
                    _weapon_00_right.Copy(itemData);
                    break;
                case ELoadoutSlot.Weapon_01_Left:
                    _weapon_01_left.Copy(itemData);
                    break;
                case ELoadoutSlot.Weapon_01_Right:
                    _weapon_01_right.Copy(itemData);
                    break;
                case ELoadoutSlot.Weapon_02_Left:
                    _weapon_02_left.Copy(itemData);
                    break;
                case ELoadoutSlot.Weapon_02_Right:
                    _weapon_02_right.Copy(itemData);
                    break;

                case ELoadoutSlot.Summon_00:
                    _summon_00.Copy(itemData);
                    break;
                case ELoadoutSlot.Summon_01:
                    _summon_01.Copy(itemData);
                    break;
                case ELoadoutSlot.Summon_02:
                    _summon_02.Copy(itemData);
                    break;
                case ELoadoutSlot.Summon_03:
                    _summon_03.Copy(itemData);
                    break;
                case ELoadoutSlot.Summon_04:
                    _summon_04.Copy(itemData);
                    break;
            }

        }
        public void SetItemAtInventorySlot(int slot, in FItemData itemData)
        {
            if (!HasStateAuthority)
                return;

            if (slot >= _inventoryItems.Length)
                return;

            _inventoryItems[slot].Copy(itemData);
        }

        public FItemData AddItemToInventory(FItemData item)
        {
            if (!item.IsValid())
            {
                Debug.LogWarning("Cannot add invalid item to inventory.");
                return item; // Return unchanged.
            }

            if (!HasStateAuthority)
            {
                Debug.LogWarning("Cannot add item to inventory without state authority.");
                return item;
            }

            FItemData remainingItem = item; // Start with full item; reduce as we place.
            int slotsChecked = 0;
            const int MAX_SLOTS_TO_CHECK = 100; // Safety cap (inventory likely small).

            var otherItemDef = Global.Tables.ItemTable.TryGetDefinition(item.DefinitionID);
            if (otherItemDef == null)
            {
                Debug.LogWarning("Cannot add item with invalid definition to inventory.");
                return item;
            }

            // Loop through inventory slots: prefer stacking, then empty.
            for (int slot = 0; slot < _inventoryItems.Length && remainingItem.IsValid() && slotsChecked < MAX_SLOTS_TO_CHECK; slot++, slotsChecked++)
            {
                var itemAtSlot = _inventoryItems[slot];
                var isSlotEmpty = !itemAtSlot.IsValid();

                if (isSlotEmpty)
                {
                    // Place entire remaining item in empty slot.
                    _inventoryItems[slot].Copy(remainingItem);
                    remainingItem = default; // All placed.
                    break; // Done.
                }
                else
                {
                    var itemAtSlotDef = Global.Tables.ItemTable.TryGetDefinition(itemAtSlot.DefinitionID);
                    if (itemAtSlotDef == null || itemAtSlotDef != otherItemDef)
                        continue; // Can't stack: skip.

                    // Special currency type check.
                    if (itemAtSlotDef is CurrencyDefinition slotCurrencyDef &&
                        otherItemDef is CurrencyDefinition otherCurrencyDef)
                    {
                        if (slotCurrencyDef.CurrencyType != otherCurrencyDef.CurrencyType)
                            continue; // Can't stack: skip.
                    }

                    // Check if can stack.
                    FItemData localSlotItem = itemAtSlot; // Copy for ref.
                    var currentStackCount = itemAtSlotDef.DataDefinition.GetStackCount(ref localSlotItem);
                    FItemData localRemainingItem = remainingItem; // Copy for ref.
                    var remainingStackCount = otherItemDef.DataDefinition.GetStackCount(ref localRemainingItem);
                    var maxStackCount = itemAtSlotDef.MaxStackCount;

                    if ((currentStackCount + remainingStackCount) <= maxStackCount)
                    {
                        // Fully stack.
                        var newTotal = currentStackCount + remainingStackCount;
                        itemAtSlotDef.DataDefinition.SetStackCount(newTotal, ref localSlotItem);
                        _inventoryItems[slot].Copy(localSlotItem);
                        remainingItem = default; // All placed.
                        break; // Done.
                    }
                    else
                    {
                        // Partial stack: fill to max, reduce remaining.
                        itemAtSlotDef.DataDefinition.SetStackCount(maxStackCount, ref localSlotItem);
                        _inventoryItems[slot].Copy(localSlotItem);
                        var excessCount = (currentStackCount + remainingStackCount) - maxStackCount;
                        otherItemDef.DataDefinition.SetStackCount(excessCount, ref localRemainingItem);
                        remainingItem.Copy(localRemainingItem); // Update remaining with excess.
                                                                // Continue to next slot with excess.
                    }
                }
            }

            if (remainingItem.IsValid())
            {
                FItemData localRemaining = remainingItem; // Copy for ref.
                var remainingStackCount = otherItemDef.DataDefinition.GetStackCount(ref localRemaining);
                Debug.LogWarning($"Could not fully place item in inventory; {remainingStackCount} of {otherItemDef.DisplayName} left over.");
            }

            return remainingItem; // Return any unplaced excess (for caller to handle, e.g., drop or add to stockpile).
        }

        public FItemData StackItem(int index, ref FItemData otherItem)
        {
            if (!otherItem.IsValid())
                return otherItem; // Nothing to stack.

            var itemAtSlot = _inventoryItems[index];
            var isSlotEmpty = !itemAtSlot.IsValid();

            var otherItemDef = Global.Tables.ItemTable.TryGetDefinition(otherItem.DefinitionID);
            if (otherItemDef == null)
                return otherItem; // Invalid other item.

            // If slot is empty, just place the whole item there (no stacking needed).
            if (isSlotEmpty)
            {
                _inventoryItems[index] = otherItem; // Assuming you have a setter method.
                return default; // Or new FItemData() for invalid/no excess.
            }

            var itemAtSlotDef = Global.Tables.ItemTable.TryGetDefinition(itemAtSlot.DefinitionID);
            if (itemAtSlotDef == null || itemAtSlotDef != otherItemDef)
                return otherItem; // Can't stack: return unchanged.

            // Special currency type check.
            if (itemAtSlotDef is CurrencyDefinition slotCurrencyDef &&
                otherItemDef is CurrencyDefinition otherCurrencyDef)
            {
                if (slotCurrencyDef.CurrencyType != otherCurrencyDef.CurrencyType)
                    return otherItem;
            }

            // Proceed with stacking.
            var currentStackCount = itemAtSlotDef.DataDefinition.GetStackCount(ref itemAtSlot);
            var otherStackCount = otherItemDef.DataDefinition.GetStackCount(ref otherItem);
            var maxStackCount = itemAtSlotDef.MaxStackCount;
            var newTotal = currentStackCount + otherStackCount;

            FItemData excessItem = default;
            if (newTotal > maxStackCount)
            {
                // Stack up to max, create excess.
                itemAtSlotDef.DataDefinition.SetStackCount(maxStackCount, ref itemAtSlot);
                excessItem = otherItem; // Copy base.
                otherItemDef.DataDefinition.SetStackCount(newTotal - maxStackCount, ref excessItem);
            }
            else
            {
                // Fully stack.
                itemAtSlotDef.DataDefinition.SetStackCount(newTotal, ref itemAtSlot);
            }

            // Update the slot with the (possibly updated) itemAtSlot.
            _inventoryItems[index] = itemAtSlot; // Assuming setter exists.

            return excessItem; // Invalid if no excess.
        }

    }

    public enum ELoadoutSlot
    { 
        None,
        Weapon_00_Left,
        Weapon_00_Right,
        Weapon_01_Left,
        Weapon_01_Right,
        Weapon_02_Left,
        Weapon_02_Right,
        Summon_00,
        Summon_01, 
        Summon_02,
        Summon_03,
        Summon_04,
        CommandSquad,
    }
}

