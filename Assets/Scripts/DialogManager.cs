using Fusion;
using LichLord.Dialog;
using LichLord.World;
using UnityEngine;

namespace LichLord
{
    public class DialogManager : ContextBehaviour
    {
        [Networked, Capacity(DialogConstants.MAX_DIALOGS)]
        [OnChangedRender(nameof(OnRep_DialogDatas))]
        protected virtual NetworkArray<FDialogData> _dialogDatas { get; }

        [SerializeField] private DialogDefinition _activeDialogDefinition;
        public DialogDefinition ActiveDialogDefinition => _activeDialogDefinition;
        
        [SerializeField] private DialogOwnerInfo _activeDialogOwnerInfo;
        public DialogOwnerInfo ActiveDialogOwnerInfo => _activeDialogOwnerInfo;

        [SerializeField] private DialogNode _activeDialogNode;
        public DialogNode LocalActiveDialogNode => _activeDialogNode;

        private int _dialogAdvanceTick;

        [SerializeField] private DialogDefinition[] _activeDialogs = new DialogDefinition[DialogConstants.MAX_DIALOGS];

        private void OnRep_DialogDatas()
        {
            for (int i = 0; i < DialogConstants.MAX_DIALOGS; i++)
            {
                _activeDialogs[i] = Global.Tables.DialogTable.TryGetDefinition(_dialogDatas.GetRef(i).DefinitionID);
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            OnRep_DialogDatas();
        }

        public override void Render()
        {
            base.Render();

            var activeDialog = Context.DialogManager.LocalActiveDialogNode;

            if (activeDialog != null)
            {
                if (!activeDialog.RequiresResponse)
                {
                    if (Runner.Tick > _dialogAdvanceTick)
                    {
                        activeDialog.InvokeAutoResponse(Context);
                    }
                }
            }
        }

        public FDialogData GetDialog(int index)
        {
            return _dialogDatas.GetRef(index);  
        }

        public DialogDefinition GetDialogDefinition(int index)
        {
            return Global.Tables.DialogTable.TryGetDefinition(GetDialog(index).DefinitionID);
        }

        public int GetFreeDialogIndex()
        {
            for (int i = 0; i < DialogConstants.MAX_DIALOGS; i++)
            {
                ref FDialogData stockpile = ref _dialogDatas.GetRef(i);
                if (!stockpile.IsAssigned) // not taken
                {
                    return i;
                }
            }
            return -1; // no free index found
        }

        // Returns index of the dialog for reference on an NPC
        public int AddActiveDialog(DialogDefinition dialog)
        {
            int freeIndex = GetFreeDialogIndex();

            if (freeIndex == -1)
            {
                Debug.Log("No Free Index");
                return -1;
            }

            ref FDialogData dialogData = ref _dialogDatas.GetRef(freeIndex);
            dialogData.DefinitionID = (ushort)dialog.TableID;
            dialogData.IsAssigned = true;
            return freeIndex;
        }

        public void LoadDialogData(FDialogSaveData dialogSave)
        {
            ref FDialogData dialogData = ref _dialogDatas.GetRef(dialogSave.index);
            dialogData = dialogSave.ToNetworkDialog();
            _dialogDatas.Set(dialogSave.index, dialogData);
        }

        public void ClearDialog(int dialogIndex)
        {
            ref FDialogData dialogData = ref _dialogDatas.GetRef(dialogIndex);

            if (ActiveDialogDefinition != null)
            {
                int localActiveDialogDefinitionID = ActiveDialogDefinition.TableID;
                if (dialogData.DefinitionID == localActiveDialogDefinitionID)
                { 
                    _activeDialogDefinition = null;
                    _activeDialogNode = null;
                }    
            }

            dialogData.DefinitionID = 0;
            dialogData.IsAssigned = false;
        }

        public void SetActiveDialogDefinition(DialogDefinition dialogDefinition)
        {
            _activeDialogDefinition = dialogDefinition;
        }

        public void SetActiveDialogOwner(DialogOwnerInfo ownerInfo)
        { 
            _activeDialogOwnerInfo = ownerInfo;
        }

        public void SetActiveDialogNode(DialogNode newDialogNode)
        {
            if (newDialogNode != null)
            {
                if (!newDialogNode.RequiresResponse)
                    _dialogAdvanceTick = Runner.Tick + newDialogNode.AdvanceTicks;
            }
            else
            { 
                SetActiveDialogDefinition(null);
            }

            _activeDialogNode = newDialogNode;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SubmitDialogAnswer(int stockpileIndex, int answerID, PlayerCharacter pc)
        {
        }
    }
}
