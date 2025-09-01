
using UnityEngine;

namespace LichLord.Dialog
{
    [CreateAssetMenu(fileName = "DialogResponse", menuName = "LichLord/Dialog/DialogResponse")]
    public class DialogResponse : ScriptableObject
    {
        [TextArea(3, 10)] // Configures the text area to show 3-10 lines
        [SerializeField]
        private string _text;
        public string Text => _text;
    }
}
