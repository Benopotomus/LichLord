using Fusion;
using LichLord.Dialog;
using LichLord.NonPlayerCharacters;
using LichLord.World;
using UnityEngine;

namespace LichLord
{
    public class DialogManager : ContextBehaviour
    {
        private const int MAX_DIALOGS = 32;

        [Networked, Capacity(MAX_DIALOGS)]
        [OnChangedRender(nameof(OnRep_DialogDatas))]
        protected virtual NetworkArray<FDialogData> _dialogDatas { get; }

        [SerializeField] private int _localActiveDialogIndex = -1; 
        public int LocalActiveDialogIndex => _localActiveDialogIndex;

        [SerializeField] private DialogNode _localActiveDialogNode;
        public DialogNode LocalActiveDialogNode => _localActiveDialogNode;

        private void OnRep_DialogDatas(NetworkBehaviourBuffer previous)
        {
        }

        public override void Spawned()
        {
            base.Spawned();

            //SpawnStaticDialog()
        }

        public FDialogData GetDialog(int index)
        {
            return _dialogDatas.GetRef(index);
        }

        public int GetFreeDialogIndex()
        {
            for (int i = 0; i < MAX_DIALOGS; i++)
            {
                ref FDialogData stockpile = ref _dialogDatas.GetRef(i);
                if (!stockpile.IsAssigned) // not taken
                {
                    return i;
                }
            }
            return -1; // no free index found
        }

        public void AssignDialogIndex(int index)
        {
            ref FDialogData dialogData = ref _dialogDatas.GetRef(index);
            //dialogData.Assign();
        }

        public void LoadDialogData(FStockpileSaveData stockpileSave)
        {
            //ref FDialogData dialogData = ref _dialogDatas.GetRef(stockpileSave.index);
            //dialogData = stockpileSave.ToNetworkStockpile();
            //_stockpileDatas.Set(stockpileSave.index, dialogData);
        }

        public void ClearDialog(int dialogIndex)
        {
            ref FDialogData dialogData = ref _dialogDatas.GetRef(dialogIndex);
            //stockpileData.ClearStockpile();
            //stockpileData.Unassign();
        }

        public void SpawnLocalStaticDialog(DialogDefinition dialogDefinition)
        {
            _localActiveDialogNode = dialogDefinition.StartingNode;
        }

        public void SetActiveDialogNode(DialogNode dialogNode)
        { 
            _localActiveDialogNode = dialogNode;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SubmitDialogAnswer(int stockpileIndex, int answerID, PlayerCharacter pc)
        {
        }
        
        public void TrySpawnDialogNPC(NonPlayerCharacterDefinition definition, Vector3 spawnPosition, ENPCSpawnType spawnType, ETeamID teamID, DialogDefinition dialogDefinition)
        {
            int freeDialogIndex = GetFreeDialogIndex();

            if (freeDialogIndex == -1)
                return;

            ref FDialogData dialogData = ref _dialogDatas.GetRef(freeDialogIndex);
            dialogData.DialogID = dialogDefinition.TableID;
            dialogData.IsAssigned = true;
            dialogData.NPCActive = true;
            
            Context.NonPlayerCharacterManager.SpawnDialogNPC(spawnPosition,
                definition,
                spawnType,
                teamID,
                EAttitude.Neutral,
                freeDialogIndex,
                true);
            
        }
        
    }
}
