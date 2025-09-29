using UnityEditor;
using UnityEngine;
using LichLord.Items;
using LichLord;

[CustomPropertyDrawer(typeof(TestSummonable))]
public class TestSummonablePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Find the Definition property
        var definitionProp = property.FindPropertyRelative("Definition");

        // Calculate positions for fields
        float lineHeight = EditorGUIUtility.singleLineHeight * 2f;
        Rect definitionRect = new Rect(position.x, position.y, position.width, lineHeight);

        // Draw Definition field, restricted to SummonableDefinition
        EditorGUI.LabelField(definitionRect, "Definition (SummonableDefinition only)");
        SummonableDefinition newDefinition = EditorGUI.ObjectField(
            definitionRect,
            definitionProp.objectReferenceValue as SummonableDefinition,
            typeof(SummonableDefinition),
            false
        ) as SummonableDefinition;

        // Update if changed
        if (newDefinition != definitionProp.objectReferenceValue)
        {
            definitionProp.objectReferenceValue = newDefinition;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2 + 4; // Two fields + spacing
    }
}