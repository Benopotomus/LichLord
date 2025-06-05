using UnityEditor;
using LichLord.NonPlayerCharacters;

namespace LichLord.Editor
{
    [CustomEditor(typeof(NonPlayerCharacterTable))]
    public class NonPlayerCharacterTableEditor : ObjectTableEditor<
        NonPlayerCharacterDefinition,
        NonPlayerCharacterTable>
    {

    }
}