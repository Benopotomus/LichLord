
using LichLord.Items;
using System;
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

        [Header("Summmon 00")]
        public TestSummonable _summon_00;

        [Header("Summmon 00")]
        public TestSummonable _summon_01;

        [Header("Summmon 00")]
        public TestSummonable _summon_02;

        [Header("Summmon 00")]
        public TestSummonable _summon_03;

        [Header("Summmon 00")]
        public TestSummonable _summon_04;

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

                case ELoadoutSlot.Summon_00:
                    return _summon_00.ToItemData();
                case ELoadoutSlot.Summon_01:
                    return _summon_01.ToItemData();
                case ELoadoutSlot.Summon_02:
                    return _summon_02.ToItemData();
                case ELoadoutSlot.Summon_03:
                    return _summon_03.ToItemData();
                case ELoadoutSlot.Summon_04:
                    return _summon_04.ToItemData();

            }
            return new FItemData();
        }
    }
}
