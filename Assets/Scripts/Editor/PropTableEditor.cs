using UnityEditor;
using LichLord.Props;

namespace LichLord.Editor
{
    [CustomEditor(typeof(PropTable))]
    public class PropTableEditor : ObjectTableEditor<
        PropDefinition,
        PropTable>
    {

    }
}