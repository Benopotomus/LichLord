using UnityEditor;
using LichLord.Buildables;

namespace LichLord.Editor
{
    [CustomEditor(typeof(BuildableTable))]
    public class BuildableTableEditor : ObjectTableEditor<
        BuildableDefinition,
        BuildableTable>
    {

    }
}