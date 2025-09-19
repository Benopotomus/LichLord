using UnityEditor;
using LichLord.Items;

namespace LichLord.Editor
{
    [CustomEditor(typeof(ItemTable))]
    public class ItemTableEditor : ObjectTableEditor<
        ItemDefinition,
        ItemTable>
    {

    }
}