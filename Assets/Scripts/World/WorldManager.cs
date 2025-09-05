using UnityEngine;

namespace LichLord.World
{
    public class WorldManager : ContextBehaviour
    {
        [SerializeField]
        private WorldSettings _worldSettings;
        public WorldSettings WorldSettings => _worldSettings;

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority)
            {
                Context.WorldSaveLoadManager.LoadWorld();
                Context.MissionManager.LoadWorldMissionProgress();
            }

            Context.ChunkManager.InitializeWorldChunks();
            Context.StrongholdManager.LoadStrongholds();

            if (HasStateAuthority)
            {
                Context.WorldSaveLoadManager.LoadNPCs();
                Context.NonPlayerCharacterManager.LoadNPCsFromSaves();
            }

            Context.PlayerSaveLoadManager.LoadPlayer();
            Context.MissionManager.LoadPlayerMissionProgress();

            Context.SpawnManager.SpawnLocalPlayer(Runner.LocalPlayer);

            Context.MissionManager.InitializeTutorialState();
            Context.MissionManager.InitializeMissionState();
        }
    }
}
