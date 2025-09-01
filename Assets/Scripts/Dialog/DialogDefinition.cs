
using UnityEngine;

namespace LichLord.Dialog
{
    [CreateAssetMenu(fileName = "DialogDefinition", menuName = "LichLord/Dialog/DialogDefinition")]
    public class DialogDefinition : TableObject
    {
        [SerializeField]
        private DialogNode _startingNode;
        public DialogNode StartingNode => _startingNode;
    }
}
