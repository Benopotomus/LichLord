#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEditor;
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [CustomPropertyDrawer(typeof(DefaultAnimationIndex))]
    internal class DefaultAnimationIndexPropertyDrawer : PropertyDrawer
    {
        public static InstancedAnimationData currentAnimation;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (currentAnimation != null)
            {
                GUIContent[] names = new GUIContent[currentAnimation.animations.Length];
                for (int i = 0; i < names.Length; ++i)
                    names[i] = new GUIContent(currentAnimation.animations[i].animationName);
                if (property.intValue > names.Length)
                    property.intValue = 0;
                property.intValue = EditorGUI.Popup(position, label, property.intValue, names);
                currentAnimation = null;
            }
            else
            {
                property.intValue = EditorGUI.LayerField(position, label, property.intValue);
            }
        }
    }
}
#endif