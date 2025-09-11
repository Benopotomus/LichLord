using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Dialog
{
    [CreateAssetMenu(fileName = "DialogNode", menuName = "LichLord/Dialog/DialogNode")]
    public class DialogNode : ScriptableObject
    {
        [SerializeField]
        private bool _requiresResponse;
        public bool RequiresResponse => _requiresResponse;

        [SerializeField] // if response isn't required, this will advance after the tick count here.
        private int _advanceTicks;
        public int AdvanceTicks => _advanceTicks;

        [SerializeField]
        private DialogStatement _statement;
        public DialogStatement Statement => _statement;

        [SerializeField]
        [SerializedDictionary("DialogResponse", "NextNode")]
        private SerializedDictionary<DialogResponse, DialogNode> _responses;
        public SerializedDictionary<DialogResponse, DialogNode> Responses => _responses;

        [SerializeField]
        [SerializedDictionary("DialogResponse", "ResponseAction")]
        private SerializedDictionary<DialogResponse, List<DialogResponseAction>> _responseActions;
        public SerializedDictionary<DialogResponse, List<DialogResponseAction>> ResponseActions => _responseActions;

        [SerializeField]
        private AutoDialogResponse _autoResponse;
        public AutoDialogResponse AutoResponse => _autoResponse;

        public void InvokeResponse(DialogResponse response, SceneContext context)
        {
            // Play response action if defined
            if (ResponseActions.TryGetValue(response, out var actions))
            {
                foreach (var responseAction in actions)
                {
                    responseAction.Invoke(context); // or your custom execution logic
                }
            }

            // Play response action if defined
            if (Responses.TryGetValue(response, out var nextNode))
            {
                context.DialogManager.SetActiveDialogNode(nextNode);
            }
        }

        public void InvokeAutoResponse(SceneContext context)
        {
            AutoResponse.Action?.Invoke(context); // or your custom execution logic
            context.DialogManager.SetActiveDialogNode(AutoResponse.NextNode);
        }
    }

    [Serializable]
    public class AutoDialogResponse
    {
        public DialogNode NextNode;
        public DialogResponseAction Action;
    }
}
