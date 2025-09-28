using LichLord.Items;
using System;

namespace LichLord
{
    [Serializable]
    public class TestItem
    {
        public ItemDefinition Definition;
        public int StackCount;

        public FItemData ToItemData()
        { 
            FItemData item = new FItemData();

            if (Definition != null)
            {
                item.DefinitionID = Definition.TableID;

                Definition.DataDefinition.SetStackCount(StackCount, ref item);
            }

            return item;
        }
    }
}
