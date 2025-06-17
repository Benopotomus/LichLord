using UnityEditor;

namespace LichLord.Editor
{
    [CustomEditor(typeof(ManeuverTable))]
    public class ManeuverTableEditor : ObjectTableEditor<
        ManeuverDefinition,
        ManeuverTable>
    {

    }
}