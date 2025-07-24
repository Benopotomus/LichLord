#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;
using UnityEditor;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [CustomEditor(typeof(InstancedAnimationBoneSyncBehaviour)), CanEditMultipleObjects]
    internal class InstancedAnimationBoneSyncBehaviourEditor : Editor
    {
        private static class GUIContents
        {
            public static readonly GUIContent boneTransform = new GUIContent("Transform to sync", "Transform that will be synchronized");
            public static readonly GUIContent boneName = new GUIContent("Bone name", "name of bone to enable synchronization");
        }
        private static readonly string[] empty = new string[0];

        private SerializedProperty boneTransform;
        private SerializedProperty boneName;

        private void OnEnable()
        {
            boneTransform = serializedObject.FindProperty(nameof(InstancedAnimationBoneSyncBehaviour.transformToSync));
            boneName = serializedObject.FindProperty(nameof(InstancedAnimationBoneSyncBehaviour.boneName));
            Undo.undoRedoPerformed += UndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedo;
        }

        private void UndoRedo()
        {
            for (int i = 0; i < targets.Length; ++i)
            {
                InstancedAnimationBoneSyncBehaviour iabsb = ((InstancedAnimationBoneSyncBehaviour)targets[i]);
                iabsb.StopSynchronization();
                iabsb.StartSynchronization();
            }
        }


        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(boneTransform, GUIContents.boneTransform);
            bool destinationTransformChanged = EditorGUI.EndChangeCheck();
            Object[] targets = this.targets;
            InstancedAnimationRenderer iar = ((InstancedAnimationBoneSyncBehaviour)targets[0]).GetComponent<InstancedAnimationRenderer>();
            bool hasDifferentAnimations = false;
            InstancedAnimationData first = iar.animationData;
            if (first == null)
                hasDifferentAnimations = true;

            for (int i = 1; i < targets.Length; ++i)
            {
                InstancedAnimationRenderer iar2 = ((InstancedAnimationBoneSyncBehaviour)targets[i]).GetComponent<InstancedAnimationRenderer>();
                if (iar2 == null || iar2.animationData == null || !iar2.animationData.IsRelativeTo(first))
                {
                    hasDifferentAnimations = true;
                    break;
                }
            }

            if (first != null && !hasDifferentAnimations)
            {
                string[] names = first.bonesNames;
                int index = 0;
                bool found = false;
                string current = boneName.stringValue;
                for (int i = 0; i < names.Length; ++i)
                    if (current == names[i])
                    {
                        index = i;
                        found = true;
                        break;
                    }
                int dest = EditorGUILayout.Popup(GUIContents.boneName, index, names);
                if (dest != index || !found)
                {
                    boneName.stringValue = names[dest];
                    destinationTransformChanged = true;
                }
            }
            else
            {
                if (targets.Length > 0)
                    EditorGUILayout.HelpBox("Unable to select bone because one or more Instanced Animation Renderers don't have selected Instanced Animation Data or used animations data are different. Select them individually", MessageType.Warning);
                else
                    EditorGUILayout.Popup(GUIContents.boneName, -1, empty);
            }
            if (destinationTransformChanged)
            {
                for (int i = 0; i < targets.Length; ++i)
                {
                    InstancedAnimationBoneSyncBehaviour iabsb = ((InstancedAnimationBoneSyncBehaviour)targets[i]);
                    iabsb.StopSynchronization();
                }
            }

            serializedObject.ApplyModifiedProperties();
            if (destinationTransformChanged)
            {
                for (int i = 0; i < targets.Length; ++i)
                {
                    InstancedAnimationBoneSyncBehaviour iabsb = ((InstancedAnimationBoneSyncBehaviour)targets[i]);
                    iabsb.StartSynchronization();
                }
            }
        }
    }
}
#endif