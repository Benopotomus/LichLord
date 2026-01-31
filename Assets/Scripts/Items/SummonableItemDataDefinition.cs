using UnityEngine;

// determines how the data of an item is unpacked.

namespace LichLord.Items
{
    [CreateAssetMenu(fileName = "ItemDataDefinition", menuName = "LichLord/Items/SummonableItemDataDefinition")]
    public class SummonableItemDataDefinition : ItemDataDefinition
    {
        protected const int VETERAN_LEVEL_BITS = 4; 
        protected const int VETERAN_LEVEL_SHIFT = 10; // Definition Bits 
        protected const int VETERAN_LEVEL_MASK = (1 << VETERAN_LEVEL_BITS) - 1;
        //14
        protected const int HEALTH_PERCENT_BITS = 7; 
        protected const int HEALTH_PERCENT_SHIFT = VETERAN_LEVEL_SHIFT + VETERAN_LEVEL_BITS;
        protected const int HEALTH_PERCENT_MASK = (1 << HEALTH_PERCENT_BITS) - 1;
        //21
        protected const int RESPAWN_PERCENT_BITS = 7;
        protected const int RESPAWN_PERCENT_SHIFT = HEALTH_PERCENT_SHIFT + HEALTH_PERCENT_BITS;
        protected const int RESPAWN_PERCENT_MASK = (1 << RESPAWN_PERCENT_BITS) - 1;
        //28

        //4 bits free!
    }


}