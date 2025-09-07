using LichLord.Dialog;
using UnityEngine;

namespace LichLord.Missions
{
    public class MissionManager : ContextBehaviour
    {
        [SerializeField]
        private bool _enableTutorial;

        // Local for players
        [SerializeField]
        private int _tutorialProgress = 0;
        public int TutorialProgress => _tutorialProgress;

        [SerializeField]
        private DialogDefinition[] _tutorialDialogs;

        // Load player save mission progress
        public void LoadPlayerMissionProgress()
        {
            FPlayerSaveData loadedPlayerData = Context.PlayerSaveLoadManager.LoadedPlayerSave;

            if (loadedPlayerData.IsValid())
            {
                SetTutorialProgress(loadedPlayerData.tutorialProgress);
            }
        }

        public void SetTutorialProgress(int newProgress)
        {
            if (!_enableTutorial)
                return;

            _tutorialProgress = newProgress;
        }

        // Load world save mission progress
        public void LoadWorldMissionProgress()
        { 
            
        }

        public void InitializeMissionState()
        {

        }

        public void InitializeTutorialState()
        {
            if (!_enableTutorial)
                return;

            if (_tutorialProgress == 0)
            {
                Context.DialogManager.SetActiveDialogDefinition(_tutorialDialogs[0]);
                Context.DialogManager.SetActiveDialogNode(_tutorialDialogs[0].StartingNode);
            }
        }

        public void NexusInteractionComplete()
        {
            if (!_enableTutorial)
                return;

            if (_tutorialProgress == 1)
            {
                Context.DialogManager.SetActiveDialogDefinition(_tutorialDialogs[1]);
                Context.DialogManager.SetActiveDialogNode(_tutorialDialogs[1].StartingNode);
            }
        }
    }
}
