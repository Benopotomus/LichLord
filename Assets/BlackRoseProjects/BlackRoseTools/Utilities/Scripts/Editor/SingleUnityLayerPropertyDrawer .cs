using UnityEditor;
using UnityEngine;

namespace BlackRoseProjects.Utility
{
    [CustomPropertyDrawer(typeof(Layer))]
    internal class SingleUnityLayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }
    }
}