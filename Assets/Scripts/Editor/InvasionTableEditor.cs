using UnityEditor;

namespace LichLord.Editor
{
    [CustomEditor(typeof(LichLord.InvasionTable))]
    public class InvasionTableEditor : ObjectTableEditor<
        InvasionDefinition,
        InvasionTable>
    {

    }
}