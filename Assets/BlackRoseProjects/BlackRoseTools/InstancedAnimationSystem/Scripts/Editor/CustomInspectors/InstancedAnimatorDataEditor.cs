#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [CustomEditor(typeof(InstancedAnimatorData))]
    internal class InstancedAnimatorDataEditor : Editor
    {
        private static class GUIContents
        {
            public static readonly GUIContent showRequiredAnimationClips = new GUIContent("Show required animations", "Show animations names that are required in order to work with Instanced Animation Renderer.");
            public static readonly GUIContent showParameters = new GUIContent("Show animator parameters");
        }

        private GUIStyle background;

        private HashSet<string> uniqueAnimationNames = new HashSet<string>();
        bool requireRefresh = true;
        bool showAnimationNames;
        bool showParameters;

        int states = 0;
        int parameters = 0;
        int anyTransitions = 0;
        int uniqueAnimations = 0;
        int totalTransitions = 0;

        private void OnEnable()
        {
            requireRefresh = true;
        }

        void Refresh()
        {
            if (!requireRefresh)
                return;
            requireRefresh = false;
            InstancedAnimatorData animatorData = (InstancedAnimatorData)target;

            background = new GUIStyle(EditorStyles.helpBox);
            background.stretchWidth = true;
            background.border = new RectOffset(5, 5, 5, 5);

            parameters = animatorData.parameters.Length;
            states = animatorData.states.Length;
            anyTransitions = animatorData.anyStateTransitions.Length;
            totalTransitions = anyTransitions;
            uniqueAnimationNames.Clear();
            for (int i = 0; i < states; ++i)
            {
                uniqueAnimationNames.Add(animatorData.states[i].animationName);
                totalTransitions += animatorData.states[i].transitions.Length;
            }
            uniqueAnimations = uniqueAnimationNames.Count;
        }

        public override void OnInspectorGUI()
        {
            Refresh();
            EditorGUILayout.HelpBox($"States: {states}\nParameters: {parameters}\nAny state Transitions: {anyTransitions}\nTotal transitions: {totalTransitions}\nUnique Animations: {uniqueAnimations}", MessageType.Info);

            showAnimationNames = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationNames, GUIContents.showRequiredAnimationClips);
            if (showAnimationNames)
            {
                EditorGUILayout.BeginVertical(background);
                foreach (string s in uniqueAnimationNames)
                    EditorGUILayout.LabelField(s);

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            showParameters = EditorGUILayout.BeginFoldoutHeaderGroup(showParameters, GUIContents.showParameters);
            if (showParameters)
            {
                EditorGUILayout.BeginVertical(background);
                InstancedAnimatorData animatorData = (InstancedAnimatorData)target;
                for (int i = 0; i < animatorData.parameters.Length; ++i)
                {
                    switch (animatorData.parameters[i].type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            EditorGUILayout.LabelField($"{animatorData.parameters[i].name} [bool]");
                            break;
                        case AnimatorControllerParameterType.Float:
                            EditorGUILayout.LabelField($"{animatorData.parameters[i].name} [float]");
                            break;
                        case AnimatorControllerParameterType.Int:
                            EditorGUILayout.LabelField($"{animatorData.parameters[i].name} [int]");
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            EditorGUILayout.LabelField($"{animatorData.parameters[i].name} [trigger]");
                            break;
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected override bool ShouldHideOpenButton()
        {
            return true;
        }

        [MenuItem("CONTEXT/Animator/Instanced Animation/Open Animator Animator Baker", true, -5000)]
        internal static bool CONTEXT_BakeAnimatorChack(MenuCommand command)
        {
            return CanOpenAnimator(command);
        }

        [MenuItem("CONTEXT/Animator/Instanced Animation/Open Animator Animator Baker", false, -5000)]
        internal static void CONTEXT_BakeAnimator(MenuCommand command)
        {
            Animator animator = command.context as Animator;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                InstancedAnimatorBaker.OpenWindowFor(animator.runtimeAnimatorController);
            }
        }

        [MenuItem("CONTEXT/Animator/Instanced Animation/Bake Animator to this path", false, -4999)]
        internal static void CONTEXT_BakeAnimatorNow(MenuCommand command)
        {
            Animator animator = command.context as Animator;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                AnimatorConverter converter = new AnimatorConverter();
                InstancedAnimatorData iad = converter.Convert(animator.runtimeAnimatorController as AnimatorController);
                if (iad != null)
                {
                    string path = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                    path = path.Substring(0, path.LastIndexOf(".")) + "_Animator.asset";
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    iad.name += "_Animator";
                    AnimatorConverter.SaveAsset(iad, path);
                    AssetDatabase.SaveAssetIfDirty(iad);
                    EditorGUIUtility.PingObject(iad);
                }
            }
        }

        [MenuItem("CONTEXT/Animator/Instanced Animation/Bake Animator to this path", true, -4999)]
        internal static bool CONTEXT_BakeAnimatorNowChack(MenuCommand command)
        {
            return CanOpenAnimator(command);
        }

        [MenuItem("CONTEXT/Animator/Instanced Animation/Bake Animator to path", false, -4998)]
        internal static void CONTEXT_BakeAnimatorNowPath(MenuCommand command)
        {
            Animator animator = command.context as Animator;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                AnimatorConverter converter = new AnimatorConverter();
                AnimatorController ac = animator.runtimeAnimatorController as AnimatorController;
                InstancedAnimatorData iad = converter.Convert(ac);
                if (iad != null)
                {
                    string path = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                    string assetName = ac.name + "_Animator";
                    path = converter.OpenSaveFileWindow("Assets/", assetName, "asset");
                    iad.name = assetName;
                    AnimatorConverter.SaveAsset(iad, path);
                    AssetDatabase.SaveAssetIfDirty(iad);
                    EditorGUIUtility.PingObject(iad);
                }
            }
        }

        [MenuItem("CONTEXT/Animator/Instanced Animation/Bake Animator to path", true, -4998)]
        internal static bool CONTEXT_BakeAnimatorNowPathChack(MenuCommand command)
        {
            return CanOpenAnimator(command);
        }

        private static bool CanOpenAnimator(MenuCommand command)
        {
            Animator animator = command.context as Animator;
            return animator != null && animator.runtimeAnimatorController != null;
        }

        //animator controller

        [MenuItem("CONTEXT/AnimatorController/Instanced Animation/Open Animator Animator Baker", false, -5000)]
        internal static void CONTEXT_BakeAnimatorController(MenuCommand command)
        {
            AnimatorController animator = command.context as AnimatorController;
            if (animator != null)
            {
                InstancedAnimatorBaker.OpenWindowFor(animator);
            }
        }

        [MenuItem("CONTEXT/AnimatorController/Instanced Animation/Bake Animator to this path", false, -4999)]
        internal static void CONTEXT_BakeAnimatorControllerNow(MenuCommand command)
        {
            AnimatorController animator = command.context as AnimatorController;
            if (animator != null)
            {
                AnimatorConverter converter = new AnimatorConverter();
                InstancedAnimatorData iad = converter.Convert(animator);
                if (iad != null)
                {
                    string path = AssetDatabase.GetAssetPath(animator);
                    path = path.Substring(0, path.LastIndexOf(".")) + "_Animator.asset";
                    iad.name += "_Animator";
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    AnimatorConverter.SaveAsset(iad, path);
                    AssetDatabase.SaveAssetIfDirty(iad);
                    EditorGUIUtility.PingObject(iad);
                }
            }
        }

        [MenuItem("CONTEXT/AnimatorController/Instanced Animation/Bake Animator to path", false, -4998)]
        internal static void CONTEXT_BakeAnimatorControllerNowPath(MenuCommand command)
        {
            AnimatorController animator = command.context as AnimatorController;
            if (animator != null)
            {
                AnimatorConverter converter = new AnimatorConverter();
                //   AnimatorController ac = animator.runtimeAnimatorController as AnimatorController;
                InstancedAnimatorData iad = converter.Convert(animator);
                if (iad != null)
                {
                    string path = AssetDatabase.GetAssetPath(animator);
                    string assetName = animator.name + "_Animator";
                    iad.name = assetName;
                    path = converter.OpenSaveFileWindow("Assets/", assetName, "asset");
                    AnimatorConverter.SaveAsset(iad, path);
                    AssetDatabase.SaveAssetIfDirty(iad);
                    EditorGUIUtility.PingObject(iad);
                }
            }
        }
    }
}
#endif