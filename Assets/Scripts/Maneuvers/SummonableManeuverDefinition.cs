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
                FItemData itemData = playerCharacter.Inventory.GetItemAtLoadoutSlot(loadoutSlot);
                SpawnNPCFromItem(playerCharacter, ref itemData, ticksSinceStart);
                playerCharacter.Inventory.SetItemAtLoadoutSlot(loadoutSlot, new FItemData());
            
            }
        }

        private void SpawnNPCFromItem(PlayerCharacter pc, ref FItemData itemData, int tick)
        {

            Vector3 targetPos = pc.Context.Camera.CachedRaycastHit.position;

            ItemDefinition itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID);

            if (itemDefinition is not SummonableDefinition summonable)
                return;

            var npcDefinition = summonable.NonPlayerCharacterDefinition;

            var nearestStronghold = pc.Context.StrongholdManager.GetNearestStronghold(targetPos);

            if (nearestStronghold == null)
                return;

            nearestStronghold.WorkerComponent.SummonWorker(targetPos, itemData);
        }
    }
}
