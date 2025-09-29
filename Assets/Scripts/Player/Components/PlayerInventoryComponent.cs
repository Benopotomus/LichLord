using LichLord.Items;

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
    }
}

