
using LichLord.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    [Serializable]
    public class TestLoadout  
    {
        [Header("Primary Left Weapon")]
        public TestWeapon _weapon_00_Left;
        [Header("Primary Right Weapon")]
        public TestWeapon _weapon_00_Right;
        [Header("Secondary Left Weapon")]
        public TestWeapon _weapon_01_Left;
        [Header("Secondary Right Weapon")]
        public TestWeapon _weapon_01_Right;
        [Header("Tertiary Left Weapon")]
        public TestWeapon _weapon_02_Left;
        [Header("Tertiary Right Weapon")]
        public TestWeapon _weapon_02_Right;

        public TestItem[] _items = new TestItem[36];

        public FItemData[] CopyInventory()
        {
            FItemData[] itemDatas = new FItemData[_items.Length];

            for (int i = 0; i < _items.Length; i++)
            {
                itemDatas[i] = _items[i].ToItemData();
            }

            return itemDatas;
        }

        public FItemData CopyLoadoutItem(ELoadoutSlot loadoutSlot)
        { 
            switch (loadoutSlot)
            {
                case ELoadoutSlot.Weapon_00_Left:
                    return _weapon_00_Left.ToItemData();
                case ELoadoutSlot.Weapon_00_Right:
                    return _weapon_00_Right.ToItemData();
                case ELoadoutSlot.Weapon_01_Left:
                    return _weapon_01_Left.ToItemData();
                case ELoadoutSlot.Weapon_01_Right:
                    return _weapon_01_Right.ToItemData();
                case ELoadoutSlot.Weapon_02_Left:
                    return _weapon_02_Left.ToItemData();
                case ELoadoutSlot.Weapon_02_Right:
                    return _weapon_02_Right.ToItemData();
            }
            return new FItemData();
        }
    }
}
