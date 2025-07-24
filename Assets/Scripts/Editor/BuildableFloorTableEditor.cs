using UnityEditor;
using LichLord.Buildables;

namespace LichLord.Editor
{
    [CustomEditor(typeof(BuildableFeatureTable))]
    public class BuildableFeatureTableEditor : ObjectTableEditor<
        BuildableFeatureDefinition,
        BuildableFeatureTable>
    {

    }
}