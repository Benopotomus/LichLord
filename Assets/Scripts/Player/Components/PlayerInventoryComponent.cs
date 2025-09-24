using LichLord.Items;

namespace LichLord
{
    public class PlayerInventoryComponent : ContextBehaviour
    {
        private FItem _weapon_00_left;
        private FItem _weapon_00_right;
        private FItem _weapon_01_left;
        private FItem _weapon_01_right;
        private FItem _weapon_02_left;
        private FItem _weapon_02_right;

        private FItem[] _inventoryItems;

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

            _inventoryItems = testLoadout.CopyInventory();
        }

        public FItem GetItemAtLoadoutSlot(ELoadoutSlot slot)
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
            }

            return new FItem();
        }

        public FItem GetItemAtInventorySlot(int slot)
        {
            if(slot >= _inventoryItems.Length)
                return new FItem();

            return _inventoryItems[slot];
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
    }
}

