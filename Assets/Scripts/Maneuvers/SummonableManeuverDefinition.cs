using Fusion;
using LichLord.Items;
using LichLord.UI;
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
            if (ticksSinceStart != _expendItemTick)
                return;
            
            FItemData itemData = playerCharacter.Inventory.GetItemAtLoadoutSlot(loadoutSlot);

            var result = TrySpawnNPCFromItem(playerCharacter, ref itemData, ticksSinceStart);

            if (result.Item1)
            {
                playerCharacter.Inventory.SetItemAtLoadoutSlot(loadoutSlot, new FItemData());
            }
            else
            {
                if (playerCharacter.Context.UI is GameplayUI gameplayUI)
                {
                    gameplayUI.HUD.ShowWarningText("Cannot Spawn", result.Item2);
                }

            }
        }

        private (bool, string) TrySpawnNPCFromItem(PlayerCharacter pc, ref FItemData itemData, int tick)
        {
            if (!itemData.IsValid())
                return (false, "Item Invalid");

            ItemDefinition itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID);

            if (itemDefinition is not SummonableItemDefinition summonable)
                return (false, "Item Invalid");

            Vector3 targetPos = pc.Context.Camera.CachedRaycastHit.position;

            var nearestStronghold = pc.Context.LairManager.GetNearestStronghold(targetPos);

            if (nearestStronghold == null)
            {
                return (false, "No Stronghold found");
            }
            
            var workerComponent = nearestStronghold.WorkerComponent;

            if (workerComponent.GetEmptyWorkerSlot() == -1)
            {
                return (false, "No Free Worker Slot");
            }

            var npcDefinition = summonable.NonPlayerCharacterDefinition;

            FWorldPosition compressedPosition = new FWorldPosition();
            compressedPosition.CopyPosition(targetPos);

            workerComponent.RPC_SummonWorker((byte)pc.PlayerIndex, compressedPosition, itemData);
            return (true, "");
        }
    }
}
