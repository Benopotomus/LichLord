using UnityEngine;

namespace LichLord.Dialog
{
    [CreateAssetMenu(fileName = "DialogStatement", menuName = "LichLord/Dialog/DialogStatement")]
    public class DialogStatement : ScriptableObject
    {
        [TextArea(3, 10)] // Configures the text area to show 3-10 lines
        [SerializeField]
        private string _text;
        public string Text => _text;
    }
}