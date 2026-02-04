using UnityEngine;

namespace LichLord.World
{
    public class WorldManager : ContextBehaviour
    {
        [SerializeField]
        private WorldSettings _worldSettings;
        public WorldSettings WorldSettings => _worldSettings;

        public bool Loaded = false;

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority)
            {
                Context.WorldSaveLoadManager.LoadWorld();
                Context.MissionManager.LoadWorldMissionProgress();
                Context.ContainerManager.LoadContainers();
                Context.ContainerManager.LoadItemSlots();
                Context.WorldSaveLoadManager.LoadNPCs();
                Context.NonPlayerCharacterManager.SpawnNPCsFromSaves();
            }

            Context.ChunkManager.InitializeWorldChunks();
            Context.LairManager.LoadLairs();
            Context.InvasionManager.LoadInvasionData();

            Context.PlayerSaveLoadManager.LoadPlayer();
            Context.MissionManager.LoadPlayerMissionProgress();

            Context.SpawnManager.SpawnLocalPlayer(Runner.LocalPlayer);

            Context.MissionManager.InitializeTutorialState();
            Context.MissionManager.InitializeMissionState();

            Loaded = true;
        }
    }
}
