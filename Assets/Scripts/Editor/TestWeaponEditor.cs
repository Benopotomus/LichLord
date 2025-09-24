using UnityEditor;
using UnityEngine;
using LichLord.Items;
using LichLord;

[CustomPropertyDrawer(typeof(TestWeapon))]
public class TestWeaponPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Find the Definition property
        var definitionProp = property.FindPropertyRelative("Definition");

        // Calculate positions for fields
        float lineHeight = EditorGUIUtility.singleLineHeight * 2f;
        Rect definitionRect = new Rect(position.x, position.y, position.width, lineHeight);

        // Draw Definition field, restricted to WeaponDefinition
        EditorGUI.LabelField(definitionRect, "Definition (WeaponDefinition only)");
        WeaponDefinition newDefinition = EditorGUI.ObjectField(
            definitionRect,
            definitionProp.objectReferenceValue as WeaponDefinition,
            typeof(WeaponDefinition),
            false
        ) as WeaponDefinition;

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