using UnityEditor;
using LichLord.Buildables;

namespace LichLord.Editor
{
    [CustomEditor(typeof(BuildableWallTable))]
    public class BuildableWallTableEditor : ObjectTableEditor<
        BuildableWallDefinition,
        BuildableWallTable>
    {

    }
}