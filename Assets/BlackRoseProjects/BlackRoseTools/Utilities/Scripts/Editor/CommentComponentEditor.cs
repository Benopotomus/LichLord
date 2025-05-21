using UnityEditor;
using UnityEngine;

namespace BlackRoseProjects.Utility
{
    [CustomEditor(typeof(CommentComponent))]
    internal class CommentComponentEditor : Editor
    {
        SerializedProperty text;
        GUIStyle background;
        bool initialized;

        [MenuItem("CONTEXT/CommentComponent/Paste", false, 500)]
        internal static void Paste(MenuCommand command)
        {
            CommentComponent data = (CommentComponent)command.context;
            SerializedObject serialized = new SerializedObject(data);
            serialized.UpdateIfRequiredOrScript();
            SerializedProperty text = serialized.FindProperty("text");
            text.stringValue = GUIUtility.systemCopyBuffer;
            serialized.ApplyModifiedProperties();
        }


        [MenuItem("CONTEXT/CommentComponent/Copy", false, 501)]
        internal static void CopyToClipboard(MenuCommand command)
        {
            CommentComponent data = (CommentComponent)command.context;
            SerializedObject serialized = new SerializedObject(data);
            serialized.UpdateIfRequiredOrScript();
            SerializedProperty text = serialized.FindProperty("text");
            GUIUtility.systemCopyBuffer = text.stringValue;
        }

        [MenuItem("CONTEXT/CommentComponent/Convert new Lines", false, 502)]
        internal static void ConvertNewLines(MenuCommand command)
        {
            CommentComponent data = (CommentComponent)command.context;
            SerializedObject serialized = new SerializedObject(data);
            serialized.UpdateIfRequiredOrScript();
            SerializedProperty text = serialized.FindProperty("text");
            string value = text.stringValue;
            value = value.Replace("\\n", "\n");
            value = value.Replace("\n\n", "\n");
            text.stringValue = value;
            serialized.ApplyModifiedProperties();
        }


        void GenerateStyles()
        {
            if (initialized)
                return;
            initialized = true;
            background = new GUIStyle(EditorStyles.helpBox);
            background.stretchWidth = true;
            background.border = new RectOffset(5, 5, 5, 5);
            background.richText = true;
            background.fontSize = 12;
        }

        private void OnEnable()
        {
            serializedObject.UpdateIfRequiredOrScript();
            text = serializedObject.FindProperty("text");
        }
        private void OnDisable()
        {
            initialized = false;
        }

        public override void OnInspectorGUI()
        {
            GenerateStyles();
            EditorGUILayout.TextArea(text.stringValue, background);
        }
    }
}