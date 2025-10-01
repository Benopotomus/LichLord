using Fusion;
using LichLord.Items;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "SummonableManeuver", menuName = "LichLord/Maneuvers/SummonableManeuverDefinition", order = 1)]
    public class SummonableManeuverDefinition : ManeuverDefinition
    {
        // Time until we fire back and expend the item.
        [SerializeField]
        private int _expendItemTick;
        public int ExpendItemTick => _expendItemTick;

        public void CheckExpiredItem(PlayerCharacter playerCharacter, ELoadoutSlot loadoutSlot, NetworkRunner runner, int ticksSinceStart)
        {
            if (ticksSinceStart > _expendItemTick)
            {
                playerCharacter.Inventory.SetItemAtLoadoutSlot(loadoutSlot, new FItemData());
            
            }
        }
    }
}
