namespace LichLord.Items
{
    using Fusion;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Size = 6)]
    public struct FItem : INetworkStruct
    {
        [FieldOffset(0)]
        private ushort _definitionId; // 2 bytes
        [FieldOffset(2)]
        private int _data; // 4 bytes

        // Constants for bit masks

        public int DefinitionID
        {
            get => _definitionId;
            set => _definitionId = (ushort)value;
        }

        public int Data
        {
            get => _data;
            set => _data = value;
        }

        public bool IsValid() => _definitionId != 0;

        public void Clear() => _definitionId = 0;

        public void Copy(in FItem copiedItem)
        { 
            _definitionId = copiedItem._definitionId;
            _data = copiedItem._data;
        }

        public bool IsEqual(in FItem otherItem)
        { 
            if(_definitionId != otherItem._definitionId) 
                return false;

            if (_data != otherItem._data)
                return false;

            return true;
        }

        /*
        public void UpdateItemClass(ref Item itemClass)
        {
            if (itemClass == null)

                if (!IsValid())
                {
                    //Debug.Log($"New Item Class - After: Null");
                    itemClass = null;
                    return;
                }

            ItemDefinition itemDef = Global.Tables.ItemTable.TryGetDefinition((int)ItemID);

            if (itemDef is WeaponActionSetDefinition)
            {
                if (!(itemClass is WeaponSet))
                    itemClass = new WeaponSet(this);
            }
            else if (itemDef is ConsumableActionSetDefinition)
            {
                if (!(itemClass is Consumable))
                    itemClass = new Consumable(this);
            }
            else if (itemDef is SkillActionSetDefinition)
            {
                if (!(itemClass is Skill))
                    itemClass = new Skill(this);

                itemClass.FromItem(this);
            }
            else if (itemDef is ArmorArmsDefinition)
            {
                if (!(itemClass is ArmorArms))
                    itemClass = new ArmorArms(this);
            }
            else if (itemDef is ArmorCloakDefinition)
            {
                if (!(itemClass is ArmorCloak))
                    itemClass = new ArmorCloak(this);
            }
            else if (itemDef is ArmorHeadDefinition)
            {
                if (!(itemClass is ArmorHead))
                    itemClass = new ArmorHead(this);
            }
            else if (itemDef is ArmorLegsDefinition)
            {
                if (!(itemClass is ArmorLegs))
                    itemClass = new ArmorLegs(this);
            }
            else if (itemDef is ArmorNecklaceDefinition)
            {
                if (!(itemClass is ArmorNecklace))
                    itemClass = new ArmorNecklace(this);
            }
            else if (itemDef is ArmorRingDefinition)
            {
                if (!(itemClass is ArmorRing))
                    itemClass = new ArmorRing(this);
            }
            else if (itemDef is ArmorTorsoDefinition)
            {
                if (!(itemClass is ArmorTorso))
                    itemClass = new ArmorTorso(this);
            }
            else
            {
                itemClass = null;
            }

            //Debug.Log($"New Item Class - After: {itemClass?.GetType().Name ?? "Null"}");
        }
        */
    }
}
