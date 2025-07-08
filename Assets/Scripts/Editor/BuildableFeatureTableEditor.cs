using UnityEditor;
using LichLord.Buildables;

namespace LichLord.Editor
{
    [CustomEditor(typeof(BuildableFloorTable))]
    public class BuildableFloorTableEditor : ObjectTableEditor<
        BuildableFloorDefinition,
        BuildableFloorTable>
    {

    }
}