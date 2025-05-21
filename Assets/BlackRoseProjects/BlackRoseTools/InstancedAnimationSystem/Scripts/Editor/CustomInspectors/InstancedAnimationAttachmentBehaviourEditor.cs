#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;
using UnityEditor;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [CustomEditor(typeof(InstancedAnimationAttachmentBehaviour)), CanEditMultipleObjects]
    internal class InstancedAnimationAttachmentBehaviourEditor : Editor
    {
        private static class GUIContents
        {
            public static readonly GUIContent attachementData = new GUIContent("Attachment Data", "Attachment data containing info about attachment. Attachments only work during play-mode");
        }
        private SerializedProperty attachementData;
        private bool requireWarmup = true;

        private void Warmup()
        {
            if (!requireWarmup)
                return;
            requireWarmup = false;
            attachementData = serializedObject.FindProperty(nameof(InstancedAnimationAttachmentBehaviour.attachementData));
        }

        private void OnEnable()
        {
            requireWarmup = true;
        }

        public override void OnInspectorGUI()
        {
            Warmup();
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(attachementData, GUIContents.attachementData);
            bool changeAttachment = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (targets.Length == 1)
            {//do checks only for single selection mode
                InstancedAnimationRenderer iar = ((InstancedAnimationAttachmentBehaviour)target).GetComponent<InstancedAnimationRenderer>();
                if (iar != null)
                {
                    if (iar.animationData == null)
                        EditorGUILayout.HelpBox("Instanced Animation Attachment require Animation Data to be be assigned into Instanced Animation Renderer!", MessageType.Warning);
                    else
                    {
                        InstancedAttachmentData iad = (InstancedAttachmentData)attachementData.objectReferenceValue;
                        if (iad != null && iar.animationData.BoneNameToId(iad.boneName) == -1)
                            EditorGUILayout.HelpBox("Current Instanced Animation Data don't have bone used by selected Attachment!", MessageType.Error);
                    }
                }
            }
            if (changeAttachment && Application.isPlaying)
            {
                for (int i = 0; i < targets.Length; ++i)
                {
                    InstancedAnimationAttachmentBehaviour iaab = (InstancedAnimationAttachmentBehaviour)targets[i];
                    iaab.Detach(iaab.animationAttachmentInstance);
                    iaab.AttachInternal(iaab.attachementData);
                }
            }
        }
    }
}
#endif