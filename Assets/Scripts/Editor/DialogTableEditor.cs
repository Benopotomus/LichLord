using UnityEditor;
using LichLord.Dialog;

namespace LichLord.Editor
{
    [CustomEditor(typeof(DialogTable))]
    public class DialogTableEditor : ObjectTableEditor<
        DialogDefinition,
        DialogTable>
    {

    }
}