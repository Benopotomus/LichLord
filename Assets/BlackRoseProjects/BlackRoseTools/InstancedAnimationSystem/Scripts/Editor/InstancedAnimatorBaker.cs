#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class AnimatorConverter
    {
        private AnimatorController animatorController;

        private AnimationClip[] clips;
        private AnimatorControllerLayer mainLayer;
        private AnimatorStateMachine stateMachine;

        private AnimatorState[] animatorStates;
        private AnimatorState defaultState;

        private AnimatorStateTransition[] anyStateTransitions;
        private AnimatorControllerParameter[] parameters;

        private void FillValues()
        {
            if (animatorController == null)
                return;
            clips = animatorController.animationClips;
            mainLayer = animatorController.layers[0];
            stateMachine = mainLayer.stateMachine;
            parameters = animatorController.parameters;

            List<AnimatorState> allStates = new List<AnimatorState>();
            List<AnimatorStateTransition> anyTransitions = new List<AnimatorStateTransition>();
            FillStatesFromMachine(stateMachine, allStates, anyTransitions);
            animatorStates = allStates.ToArray();

            defaultState = stateMachine.defaultState;
            anyStateTransitions = anyTransitions.ToArray();
        }

        private void FillStatesFromMachine(AnimatorStateMachine machine, List<AnimatorState> states, List<AnimatorStateTransition> transitions)
        {
            ChildAnimatorState[] objStates = machine.states;
            transitions.AddRange(machine.anyStateTransitions);
            for (int i = 0; i < objStates.Length; ++i)
            {
                states.Add(objStates[i].state);
            }
            ChildAnimatorStateMachine[] childs = machine.stateMachines;
            for (int i = 0; i < childs.Length; ++i)
            {
                FillStatesFromMachine(childs[i].stateMachine, states, transitions);
            }
        }

        internal static InstancedAnimatorData SaveAsset(InstancedAnimatorData bakedAnimator, string path)
        {
            if (bakedAnimator == null || string.IsNullOrEmpty(path))
                return null;
            return Utility.BRPAssetsHelper.CreateAsset(bakedAnimator, path);
        }

        internal InstancedAnimatorData Convert(AnimatorController animatorController)
        {
            this.animatorController = animatorController;
            FillValues();

            InstancedAnimatorData bakedAnimator = ScriptableObject.CreateInstance<InstancedAnimatorData>();
            bakedAnimator.parameters = new BakedParameters[parameters.Length];

            bakedAnimator.states = new BakedState[animatorStates.Length];
            bakedAnimator.anyStateTransitions = new BakedTransition[anyStateTransitions.Length];


            //param
            for (int i = 0; i < parameters.Length; ++i)
                bakedAnimator.parameters[i] = ConvertParameter(parameters[i]);

            //states
            for (int i = 0; i < animatorStates.Length; ++i)
                bakedAnimator.states[i] = ConvertState(animatorStates[i], i);

            //transitions for any
            for (int i = 0; i < anyStateTransitions.Length; ++i)
                bakedAnimator.anyStateTransitions[i] = ConvertTransition(anyStateTransitions[i]);

            //transitions for states
            for (int i = 0; i < bakedAnimator.states.Length; ++i)
            {
                BakedState bakedState = bakedAnimator.states[i];
                AnimatorStateTransition[] transitions = animatorStates[i].transitions;
                for (int j = 0; j < bakedState.transitions.Length; ++j)
                {
                    bakedState.transitions[j] = ConvertTransition(transitions[j]);
                }
            }

            bakedAnimator.defaultState = FindStateID(defaultState);
            return bakedAnimator;
        }

        private BakedState ConvertState(AnimatorState state, int id)
        {
            BakedState bakedState = new BakedState();

            bakedState.animationName = state.motion != null ? state.motion.name : "";
            bakedState.transitions = new BakedTransition[state.transitions.Length];
            bakedState.stateID = id;


            bakedState.animationSpeed = state.speed;
            bakedState.speedParameterActive = state.speedParameterActive;
            bakedState.speedParameter = FindParameterID(state.speedParameter);

            return bakedState;
        }

        private BakedParameters ConvertParameter(AnimatorControllerParameter para)
        {
            BakedParameters parameter = new BakedParameters();
            parameter.name = para.name;
            parameter.type = para.type;
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                case AnimatorControllerParameterType.Trigger:
                    parameter.value = para.defaultBool ? 1f : 0f;
                    break;
                case AnimatorControllerParameterType.Float:
                    parameter.value = para.defaultFloat;
                    break;
                case AnimatorControllerParameterType.Int:
                    parameter.value = para.defaultInt;
                    break;

            }
            return parameter;
        }

        private BakedTransition ConvertTransition(AnimatorStateTransition ast)
        {
            BakedTransition transition = new BakedTransition();
            List<int> triggerConditions = new List<int>();

            transition.conditions = new BakedCondition[ast.conditions.Length];

            if (ast.destinationState != null)
                transition.targetState = FindStateID(ast.destinationState);
            else if (ast.isExit)
                transition.targetState = FindStateID(defaultState);
            else if (ast.destinationStateMachine != null)
                transition.targetState = FindStateID(ast.destinationStateMachine.defaultState);

            transition.duration = ast.duration;
            transition.exitTime = ast.exitTime;
            transition.hasExitTime = ast.hasExitTime;
            transition.offset = ast.offset;
            transition.canTransitToSelf = ast.canTransitionToSelf;
            transition.fixedDuration = ast.hasFixedDuration;
            transition.interruptionSource = (TransitionInterraption)(int)ast.interruptionSource;//uncheck conversion!

            for (int j = 0; j < ast.conditions.Length; ++j)
            {
                BakedCondition bakedCondition = new BakedCondition();
                AnimatorCondition condition = ast.conditions[j];
                bakedCondition.condition = (ConditionMode)(int)condition.mode;//uncheck conversion!
                if (bakedCondition.condition == ConditionMode.If)
                {//can be trigger!
                    if (FindParameterType(condition.parameter) == AnimatorControllerParameterType.Trigger)
                    {
                        bakedCondition.condition = ConditionMode.Trigger;
                        triggerConditions.Add(j);
                    }
                }

                bakedCondition.value = condition.threshold;
                bakedCondition.parameterID = FindParameterID(condition.parameter);
                transition.conditions[j] = bakedCondition;
                transition.triggerConditions = triggerConditions.ToArray();
            }
            return transition;
        }

        private int FindParameterID(string parameterName)
        {
            for (int i = 0; i < parameters.Length; ++i)
                if (parameters[i].name == parameterName)
                    return i;
            return -1;
        }

        private AnimatorControllerParameterType FindParameterType(string parameterName)
        {
            for (int i = 0; i < parameters.Length; ++i)
                if (parameters[i].name == parameterName)
                    return parameters[i].type;
            return 0;
        }

        private int FindStateID(AnimatorState state)
        {
            for (int i = 0; i < animatorStates.Length; ++i)
                if (animatorStates[i] == state)
                    return i;
            return -1;
        }

        public string OpenSaveFileWindow(string folder, string name, string type)
        {
            string newPath = "";
            string path = EditorUtility.SaveFilePanel("Save as...", folder, name, type);
            if (path.Length > 0)
            {
                if (path.Contains(Application.dataPath))
                {
                    string s = path;
                    string d = Application.dataPath + "/";
                    string p = "Assets/" + s.Remove(0, d.Length);
                    newPath = p;
                }
                else
                {
                    Debug.LogError("Path is outside project: " + path);
                }
            }
            return newPath;
        }
    }

    internal class InstancedAnimatorBaker : EditorWindow
    {
        private static InstancedAnimatorBaker managerWindow;
        [SerializeField] private static AnimatorController animatorController;
        private static RuntimeAnimatorController runtimeAnimatorController;

        private static string destinationPath;

        private int clips;
        private int layers;
        private int parameters;

        private bool hasFootIK;
        private bool hasMirror;
        private bool hasCycleOffset;
        private bool hasBlendTree;

        private static class GUIContents
        {
            public static readonly GUIContent animationController = new GUIContent("Animator Controller", "Unity Animator Controller that will be Baked");
            public static readonly GUIContent bake = new GUIContent("Bake Animator", "Bake selected Animator");
            public static readonly GUIContent helpButton = new GUIContent(EditorGUIUtility.IconContent("_Help@2x"));

            static GUIContents()
            {
                helpButton.tooltip = "Open online documentation";
            }
        }

        [MenuItem("Tools/Black Rose Projects/Instanced Animation System/Animator Baker", false, 1)]
        private static void MakeWindow()
        {
            managerWindow = GetWindow<InstancedAnimatorBaker>("Animator Baker");
            managerWindow.minSize = new Vector2(350, 150);
        }

        internal static void OpenWindowFor(RuntimeAnimatorController animator)
        {
            MakeWindow();
            runtimeAnimatorController = animator;
            animatorController = runtimeAnimatorController as AnimatorController;
            managerWindow.FillValues();
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            RuntimeAnimatorController rac = EditorGUILayout.ObjectField(GUIContents.animationController, runtimeAnimatorController, typeof(RuntimeAnimatorController), false) as RuntimeAnimatorController;
            if (GUILayout.Button(GUIContents.helpButton, EditorStyles.toolbarButton, GUILayout.MaxWidth(30)))
                Application.OpenURL("http://docs.blackrosetools.com/InstancedAnimations/html/window_animator.html");
            GUILayout.EndHorizontal();


            if (rac != runtimeAnimatorController)
            {//refresh
                runtimeAnimatorController = rac;
                animatorController = runtimeAnimatorController as AnimatorController;
                FillValues();
            }

            if (animatorController != null)
            {
                DrawAnimationController();
                if (GUILayout.Button(GUIContents.bake))
                {
                    string path = GetTargetPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        AnimatorConverter converter = new AnimatorConverter();
                        InstancedAnimatorData animator = converter.Convert(animatorController);
                        string namePath = path.Substring(path.LastIndexOf("/"));
                        namePath = namePath.Substring(0, namePath.LastIndexOf("."));
                        animator.name = namePath;
                        AnimatorConverter.SaveAsset(animator, path);
                        AssetDatabase.SaveAssets();
                        EditorGUIUtility.PingObject(animator);
                    }
                }
            }
        }

        private void FillValues()
        {
            if (animatorController == null)
                return;
            clips = animatorController.animationClips.Length;
            layers = animatorController.layers.Length;
            parameters = animatorController.parameters.Length;

            hasFootIK = false;
            hasMirror = false;
            hasCycleOffset = false;
            hasBlendTree = false;

            List<AnimatorState> states = new List<AnimatorState>();

            FillFromMachine(states, animatorController.layers[0].stateMachine);

            for (int i = 0; i < states.Count; ++i)
            {
                AnimatorState state = states[i];
                if (state.iKOnFeet)
                    hasFootIK = true;
                if (state.mirror || state.mirrorParameterActive)
                    hasMirror = true;
                if (state.cycleOffsetParameterActive)
                    hasCycleOffset = true;
                if (state.motion != null && state.motion.GetType() == typeof(BlendTree))
                    hasBlendTree = true;

            }
        }

        private void FillFromMachine(List<AnimatorState> states, AnimatorStateMachine asm)
        {
            ChildAnimatorState[] objStates = asm.states;
            for (int i = 0; i < objStates.Length; ++i)
                states.Add(objStates[i].state);
            ChildAnimatorStateMachine[] casm = asm.stateMachines;
            for (int i = 0; i < casm.Length; ++i)
                FillFromMachine(states, casm[i].stateMachine);
        }

        void DrawAnimationController()
        {
            if (layers > 1)
                EditorGUILayout.HelpBox($"Selected animator contains more than one layer. Only one layer can be baked into Baked Animator", MessageType.Warning);
            if (hasFootIK)
                EditorGUILayout.HelpBox($"Selected animator contains one or more Foot IK settings enabled. This is not supported for animation baking and will be ignored", MessageType.Warning);
            if (hasMirror)
                EditorGUILayout.HelpBox($"Selected animator contains one or more Mirror settings enabled. This is not supported for animation baking and will be ignored", MessageType.Warning);
            if (hasCycleOffset)
                EditorGUILayout.HelpBox($"Selected animator contains one or more parameter dependedt CycleOffset settings. This is not supported for animation baking and will be ignored", MessageType.Warning);
            if (hasBlendTree)
                EditorGUILayout.HelpBox($"Selected animator contains one or blend tree. This is not supported for animation baking and will be ignored", MessageType.Warning);
            EditorGUILayout.HelpBox($"Animation clips: {clips}\nParameters: {parameters}", MessageType.Info);
        }

        public string GetTargetPath()
        {
            string folder = "Assets";
            if (animatorController != null)
                folder = AssetDatabase.GetAssetPath(animatorController);
            string name = animatorController.name + "_Animator";

            if (!string.IsNullOrEmpty(destinationPath))
            {
                if (destinationPath.Contains('/'))
                {
                    folder = destinationPath.Substring(0, destinationPath.LastIndexOf('/'));
                    if (destinationPath.EndsWith(".asset"))
                        name = destinationPath.Substring(folder.Length + 1, destinationPath.Length - 7 - folder.Length);
                    else
                        name = destinationPath.Substring(folder.Length + 1);
                }
                else
                {
                    folder = "Assets";
                    if (destinationPath.EndsWith(".asset"))
                        name = destinationPath.Substring(destinationPath.Length - 6);
                    else
                        name = destinationPath;
                }
            }

            return OpenSaveFileWindow(folder, name, "asset");
        }

        public static string OpenSaveFileWindow(string folder, string name, string type)
        {
            string newPath = "";
            string path = EditorUtility.SaveFilePanel("Save as...", folder, name, type);
            if (path.Length > 0)
            {
                if (path.Contains(Application.dataPath))
                {
                    string s = path;
                    string d = Application.dataPath + "/";
                    string p = "Assets/" + s.Remove(0, d.Length);
                    newPath = p;
                }
                else
                {
                    Debug.LogError("Path is outside project: " + path);
                }
            }
            return newPath;
        }
    }
}
#endif