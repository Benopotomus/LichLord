using UnityEditor;
using LichLord.Props;

namespace LichLord.Editor
{
    [CustomEditor(typeof(LichLord.Props.PropTable))]
    public class PropTableEditor : ObjectTableEditor<
        PropDefinition,
        PropTable>
    {

    }
}