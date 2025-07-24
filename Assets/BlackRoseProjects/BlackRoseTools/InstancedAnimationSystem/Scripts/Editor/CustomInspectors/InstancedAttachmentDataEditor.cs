#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [CustomEditor(typeof(InstancedAttachmentData))]
    internal class InstancedAttachmentDataEditor : Editor
    {
        private const string ContextPath = "CONTEXT/InstancedAttachmentData/";
        [MenuItem(ContextPath + "Paste Attachment data", false, 151)]
        internal static void PasteValues(MenuCommand command)
        {
            InstancedAttachmentData data = (InstancedAttachmentData)command.context;
            string name = data.name;
            string json = GUIUtility.systemCopyBuffer;
            Undo.RecordObject(data, "paste data");
            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, data);
            }
            catch (System.ArgumentException)
            {
                return;
            }
            data.name = name;
            EditorUtility.SetDirty(data);
        }

        [MenuItem(ContextPath + "Paste Attachment data", true, 151)]
        internal static bool PasteValuesCheck(MenuCommand command)
        {
            string json = GUIUtility.systemCopyBuffer;
            InstancedAttachmentData v = CreateInstance<InstancedAttachmentData>();
            bool result;
            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, v);
                result = true;
            }
            catch (System.ArgumentException)
            {
                result = false;
            }
            DestroyImmediate(v);
            return result;
        }

        [MenuItem(ContextPath + "Copy Attachment data", false, 150)]
        internal static void CopyToClipboard(MenuCommand command)
        {
            InstancedAttachmentData data = (InstancedAttachmentData)command.context;
            GUIUtility.systemCopyBuffer = EditorJsonUtility.ToJson(data, false);
        }

        [OnOpenAsset]
        internal static bool OnOpenAsset(int instanceID, int line)
        {
            if (InstancedRendererConfigurator.IsOpenAndReady())
            {
                Object target = EditorUtility.InstanceIDToObject(instanceID);

                if (target is InstancedAttachmentData)
                {
                    InstancedRendererConfigurator.OpenAttachmentConfig((InstancedAttachmentData)target);
                    return true;
                }
            }
            return false;
        }

        protected override bool ShouldHideOpenButton()
        {
            return !InstancedRendererConfigurator.IsOpenAndReady();
        }
    }
}
#endif