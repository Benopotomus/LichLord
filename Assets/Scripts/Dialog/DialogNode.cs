using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Dialog
{
    [CreateAssetMenu(fileName = "DialogNode", menuName = "LichLord/Dialog/DialogNode")]
    public class DialogNode : ScriptableObject
    {
        [SerializeField]
        private DialogStatement _statement;
        public DialogStatement Statement => _statement;

        [SerializeField]
        [SerializedDictionary("DialogResponse", "NextNode")]
        private SerializedDictionary<DialogResponse, DialogNode> _responses;
        public SerializedDictionary<DialogResponse, DialogNode> Responses => _responses;
    }
}
