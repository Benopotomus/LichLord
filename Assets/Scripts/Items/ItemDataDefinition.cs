using UnityEngine;

// determines how the data of an item is unpacked.

namespace LichLord.Items
{
    [CreateAssetMenu(fileName = "ItemDataDefinition", menuName = "LichLord/Items/ItemDataDefinition")]
    public class ItemDataDefinition : ScriptableObject
    {

        public virtual void InitializeData(ref FItem itemData, ItemDefinition definition)
        {

        }

        // Stack Count
        public virtual int GetStackCount(ref FItem itemData)
        {
            return 1;
        }

        public virtual void SetStackCount(int count, ref FItem itemData)
        {

        }

    }


}