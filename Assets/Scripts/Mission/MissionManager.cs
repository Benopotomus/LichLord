
namespace LichLord.Missions
{
    public class MissionManager : ContextBehaviour
    {
        // Local for players
        private int _tutorialProgress = 0;
        public int TutorialProgress => _tutorialProgress;

        // Load player save mission progress
        public void LoadPlayerMissionProgress()
        {
            FPlayerSaveData loadedPlayerData = Context.PlayerSaveLoadManager.LoadedPlayerSave;

            if (loadedPlayerData.IsValid())
            {
                _tutorialProgress = loadedPlayerData.tutorialProgress;
            }
        }

        // Load world save mission progress
        public void LoadWorldMissionProgress()
        { 
            
        }
    }
}
