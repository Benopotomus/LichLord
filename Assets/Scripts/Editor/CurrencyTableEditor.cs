using UnityEditor;

namespace LichLord.Editor
{
    [CustomEditor(typeof(CurrencyTable))]
    public class CurrencyTableEditor : ObjectTableEditor<
        CurrencyDefinition,
        CurrencyTable>
    {

    }
}