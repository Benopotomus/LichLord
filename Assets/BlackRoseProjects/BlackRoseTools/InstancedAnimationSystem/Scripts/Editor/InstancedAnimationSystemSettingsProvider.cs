using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Helper = BlackRoseProjects.InstancedAnimationSystem.InstancedAnimationHelper;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class InstancedAnimationSystemSettingsProvider : SettingsProvider
    {
        private class GUIContents
        {
            public static readonly GUIContent generalSettings = new GUIContent("General settings");
            public static readonly GUIContent editorSettings = new GUIContent("Editor settings");
            public static readonly GUIContent assemblySettings = new GUIContent("Assembly settings");
            public static readonly GUIContent initAtStartup = new GUIContent("Auto initialize", "Automatically initialize Instanced Animation System at application start. This can prevent spike at creating first Instanced Renderer");
            public static readonly GUIContent renderOnlyToCurrentCamera = new GUIContent("Render only to Current Camera", "Switch between rendering only to Current Camera or to all active cameras");
            public static readonly GUIContent instancingPackageSize = new GUIContent("Instancing package size", "Max number of rendered instances in single batch");
            public static readonly GUIContent maxInstancedObjects = new GUIContent("Max instanced objects", "Total count of objects that can be rendered by Instanced Animation System");
            public static readonly GUIContent renderInstancesDuringPause = new GUIContent("Render during pause", "Will render Instances in scene View during editor pause. This can cause heavy GPU usage during pause (have no impact on performance during playmode or in build)");
            public static readonly GUIContent drawBoundingGizmoSpheres = new GUIContent("Draw bounding gizmo", "Will draw gizmos of bounding spheres when selecting Instanced Renderer Behaviour in scene View");
            public static readonly GUIContent selectionOutlineColor = new GUIContent("Selection color", "Outline color for selected Instanced Animation Renderers");
            public static readonly GUIContent selectionOutlineActive = new GUIContent("Selection outline", "If selected Instanced Animation Renderers should have outline in scene view");
            public static readonly GUIContent editorRenderMode = new GUIContent("Render without playmode", "Options for how instances should be rendered without playmode in scene and game view");
            public static readonly GUIContent customShaderValues = new GUIContent("Custom Shader values", "Enable support to custom shaders property blocks, that allow usage of floats and vectors for them in InstancedRenderers. Enabling this option generate a small overhead even if renderer not have enabled custom shader values");
            public static readonly GUIContent instancedProfiling = new GUIContent("Instanced profiling", "Enable additional profiling samplers that can be helpfull in debuging, but create additional performance overhead");
            public static readonly GUIContent animatorSafetyChecks = new GUIContent("Animator safety checks", "Enable additional checks for index ranges and null checks for animators");
            public static readonly GUIContent transitionsBlending = new GUIContent("Transitions blending", "This option will enable normal and tangents blending for transitions in delivered shaders. Usually normal blending is not necessary because of transitions being fast enough to notice any glitches. Enabling this option will increase only GPU load");
          //  public static readonly GUIContent documentationButton = new GUIContent("Documentation", "Open online documentation");

            public static readonly GUIContent helpButton = new GUIContent(EditorGUIUtility.IconContent("_Help@2x"));

            static GUIContents()
            {
                helpButton.tooltip = "Open online documentation";
            }
        }

        private SerializedObject m_CustomSettings;

        private SerializedProperty initAtStartup;
        private SerializedProperty renderOnlyToCurrentCamera;
        private SerializedProperty maxInstancedObjects;
        private SerializedProperty instancingPackageSize;
        private SerializedProperty renderInstancesDuringPause;
        private SerializedProperty drawBoundingGizmoSpheres;
        private SerializedProperty selectionOutlineActive;
        private SerializedProperty editorRenderMode;
        private SerializedProperty transitionsBlending;
        private SerializedProperty selectionOutlineColor;

        static string path = Application.dataPath + "/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Scripts/InstancedRendering.asmdef";
        static string path2 = Application.dataPath + "/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Scripts/Editor/InstancedRenderingEditor.asmdef";

        static bool BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES;
        static bool BLACKROSE_INSTANCING_PROFILING;
        static bool BLACKROSE_INSTANCING_SAFETY_CHECKS;

        static bool isReloading = false;

        bool wasWarmup = false;
        private GUIStyle lodBackground;
        private GUIStyle labelHeaderStyle;

        public InstancedAnimationSystemSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_CustomSettings = GetSerializedSettings();
            path = Application.dataPath + "/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Scripts/InstancedAnimationSystem.asmdef";
            path2 = Application.dataPath + "/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Scripts/Editor/InstancedAnimationSystemEditor.asmdef";

            BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES = Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES);
            BLACKROSE_INSTANCING_PROFILING = Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_PROFILING);
            BLACKROSE_INSTANCING_SAFETY_CHECKS = Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_SAFETY_CHECKS);
            isReloading = false;

            initAtStartup = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.initAtStartup));
            renderOnlyToCurrentCamera = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.renderOnlyToCurrentCamera));
            maxInstancedObjects = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.maxInstancedObjects));
            instancingPackageSize = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.instancingPackageSize));
            renderInstancesDuringPause = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.renderInstancesDuringPause));
            drawBoundingGizmoSpheres = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.drawBoundingGizmoSpheres));
            editorRenderMode = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.editorRenderMode));
            transitionsBlending = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.transitionsBlending));
            selectionOutlineColor = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.selectionOutlineColor));
            selectionOutlineActive = m_CustomSettings.FindProperty(nameof(InstancedAnimationSystemSettings.selectionOutlineActive));
        }

        public override void OnTitleBarGUI()
        {
            base.OnTitleBarGUI();
           if (GUILayout.Button(GUIContents.helpButton, EditorStyles.iconButton))
                Application.OpenURL("http://docs.blackrosetools.com/InstancedAnimations/html/index.html");
        }

        private static InstancedAnimationSystemSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<InstancedAnimationSystemSettings>(InstancedAnimationSystemSettings.settingsFullPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<InstancedAnimationSystemSettings>();
                settings.FillDefaultValues();
                AssetDatabase.CreateAsset(settings, InstancedAnimationSystemSettings.settingsFullPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        void Warmup()
        {
            if (wasWarmup)
                return;
            wasWarmup = true;
            lodBackground = new GUIStyle(EditorStyles.helpBox);
            lodBackground.stretchWidth = true;
            lodBackground.border = new RectOffset(5, 5, 5, 5);

            labelHeaderStyle = new GUIStyle(GUI.skin.label);
            labelHeaderStyle.fontStyle = FontStyle.Bold;
        }

        public override void OnGUI(string searchContext)
        {
            if (DrawBaseConfig())
                DrawDefault();
        }

        private bool DrawBaseConfig()
        {
            bool hasCollections = Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_COLLECTIONS);
            bool hasMath = Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_MATH);
            bool hasBurst = Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_BURST);


            if (!hasCollections || !hasMath)
            {
                EditorGUILayout.HelpBox($"Not found required packages:{(!hasCollections ? "Unity.Collections " : " ")}{(!hasMath ? "Unity.Mathematics " : " ")}Instanced Animation System unable to initialize", MessageType.Error);
                if (GUILayout.Button("Install Required packages"))
                    Utility.BRPPackageHelper.InstallPackages(new string[] { "com.unity.collections@1.2.4", "com.unity.mathematics" });
            }
            return hasCollections && hasMath;
        }

        private void DrawDefault()
        {
            Warmup();
            m_CustomSettings.UpdateIfRequiredOrScript();
            EditorGUIUtility.labelWidth = 250;

            //general settings
            EditorGUILayout.LabelField(GUIContents.generalSettings, labelHeaderStyle);
            EditorGUI.indentLevel++;
            GUI.enabled = !Application.isPlaying;
            EditorGUILayout.PropertyField(initAtStartup, GUIContents.initAtStartup);
            EditorGUILayout.PropertyField(instancingPackageSize, GUIContents.instancingPackageSize);
            if ((Utility.BRPPipelineHelper.GetCurrentPipeline() == Utility.Pipelines.HDRP) && instancingPackageSize.intValue > 454)
                EditorGUILayout.HelpBox("High Definition Render Pipeline (HDRP) is supporting up to 454 objects per batch. Using higher value will cause Unity to automatically split too big batches into smaller ones causing batches fragmentation", MessageType.Warning);
            EditorGUILayout.PropertyField(maxInstancedObjects, GUIContents.maxInstancedObjects);
            EditorGUILayout.PropertyField(renderOnlyToCurrentCamera, GUIContents.renderOnlyToCurrentCamera);
            EditorGUILayout.PropertyField(transitionsBlending, GUIContents.transitionsBlending);
            GUI.enabled = true;
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);

            //editor settings
            EditorGUILayout.LabelField(GUIContents.editorSettings, labelHeaderStyle);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            bool reloadTransitionOption = EditorGUI.EndChangeCheck();
            EditorGUILayout.PropertyField(renderInstancesDuringPause, GUIContents.renderInstancesDuringPause);
            EditorGUILayout.PropertyField(editorRenderMode, GUIContents.editorRenderMode);
            EditorGUILayout.PropertyField(drawBoundingGizmoSpheres, GUIContents.drawBoundingGizmoSpheres);
            EditorGUILayout.PropertyField(selectionOutlineActive, GUIContents.selectionOutlineActive);
            if (selectionOutlineActive.boolValue)
                EditorGUILayout.PropertyField(selectionOutlineColor, GUIContents.selectionOutlineColor);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);

            m_CustomSettings.ApplyModifiedProperties();
            if (reloadTransitionOption)
            {
                GlobalKeyword keyword = GlobalKeyword.Create(Helper.INSTANCING_NORMAL_TRANSITION_BLENDING);
                if (transitionsBlending.boolValue)
                    Shader.EnableKeyword(keyword);
                else
                    Shader.DisableKeyword(keyword);
            }

            //defines
            EditorGUILayout.LabelField(GUIContents.assemblySettings, labelHeaderStyle);
            EditorGUI.indentLevel++;
            BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES = DefineField(BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES, GUIContents.customShaderValues);
            BLACKROSE_INSTANCING_PROFILING = DefineField(BLACKROSE_INSTANCING_PROFILING, GUIContents.instancedProfiling);
            BLACKROSE_INSTANCING_SAFETY_CHECKS = DefineField(BLACKROSE_INSTANCING_SAFETY_CHECKS, GUIContents.animatorSafetyChecks);

            if (BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES != Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES) ||
                BLACKROSE_INSTANCING_PROFILING != Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_PROFILING) ||
                BLACKROSE_INSTANCING_SAFETY_CHECKS != Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_SAFETY_CHECKS))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.enabled = !isReloading && !Application.isPlaying;
                if (GUILayout.Button("Apply"))
                {
                    ApplyAssemblyChanges();
                }
                if (GUILayout.Button("Revert"))
                {
                    BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES = Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES);
                    BLACKROSE_INSTANCING_PROFILING = Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_PROFILING);
                    BLACKROSE_INSTANCING_SAFETY_CHECKS = Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_SAFETY_CHECKS);
                }
                GUILayout.EndHorizontal();
                if (isReloading)
                    EditorGUILayout.HelpBox("Scripts are reloading...", MessageType.None);
                GUI.enabled = true;
            }
            EditorGUI.indentLevel--;
        }

        private static bool DefineField(bool value, GUIContent content)
        {
            bool define = value;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content);
            GUI.enabled = !define && !isReloading;
            if (GUILayout.Button(define ? "Enabled" : "Enable", GUILayout.MaxWidth(150), GUILayout.MinWidth(100)))
                value = true;
            GUI.enabled = define && !isReloading;
            if (GUILayout.Button(!define ? "Disabled" : "Disable", GUILayout.MaxWidth(150), GUILayout.MinWidth(100)))
                value = false;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        private static void ProcessAssemblyFile(string path, List<string> defToAdd, List<string> defToRemove)
        {
            List<string> input = new List<string>(System.IO.File.ReadAllLines(path));
            for (int i = 0; i < defToAdd.Count; ++i)
                input = AddDefinitionNew(input, defToAdd[i]);
            for (int i = 0; i < defToRemove.Count; ++i)
                input = RemoveDefinitionNew(input, defToRemove[i]);
            System.IO.File.WriteAllLines(path, input.ToArray());
        }

        private static void ApplyAssemblyChanges()
        {
            List<string> toAdd = new List<string>();
            List<string> toRemove = new List<string>();

            if (BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES != Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES))
            {
                isReloading = true;
                if (BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES)
                    toAdd.Add(Helper.BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES);
                else
                    toRemove.Add(Helper.BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES);
            }
            if (BLACKROSE_INSTANCING_PROFILING != Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_PROFILING))
            {
                isReloading = true;
                if (BLACKROSE_INSTANCING_PROFILING)
                    toAdd.Add(Helper.BLACKROSE_INSTANCING_PROFILING);
                else
                    toRemove.Add(Helper.BLACKROSE_INSTANCING_PROFILING);
            }

            if (BLACKROSE_INSTANCING_SAFETY_CHECKS != Helper.HasDefinition(Helper.BLACKROSE_INSTANCING_SAFETY_CHECKS))
            {
                isReloading = true;
                if (BLACKROSE_INSTANCING_SAFETY_CHECKS)
                    toAdd.Add(Helper.BLACKROSE_INSTANCING_SAFETY_CHECKS);
                else
                    toRemove.Add(Helper.BLACKROSE_INSTANCING_SAFETY_CHECKS);
            }
            ProcessAssemblyFile(path, toAdd, toRemove);
            ProcessAssemblyFile(path2, toAdd, toRemove);
            AssetDatabase.Refresh();
        }


        private static List<string> RemoveDefinitionNew(List<string> input, string definition)
        {
            List<string> output = new List<string>();

            bool insideDefines = false;
            int status = 1;
            for (int i = 0; i < input.Count; ++i)
            {
                string s = input[i];
                if (!insideDefines)
                {
                    if (s.Contains("    \"versionDefines\": [],"))
                    {

                    }
                    else if (s.Contains("\"versionDefines\""))
                    {
                        insideDefines = true;
                        output.Add(s);
                    }
                    else
                        output.Add(s);
                }
                else
                {
                    if (s.Contains("],"))
                    {
                        insideDefines = false;
                        output.Add(s);
                    }
                    else
                    {
                        status = RemoveDefine(input, ref i, output, definition);
                        if (status == -2)//already exist!
                            return input;
                        if (status == 0)
                            insideDefines = false;
                    }
                }
            }
            return output;
        }

        private static List<string> AddDefinitionNew(List<string> input, string definition)
        {
            List<string> output = new List<string>();

            bool insideDefines = false;
            int status = 1;
            for (int i = 0; i < input.Count; ++i)
            {
                string s = input[i];
                if (!insideDefines)
                {
                    if (s.Contains("    \"versionDefines\": [],"))
                    {
                        output.Add("    \"versionDefines\": [");
                        output.Add("        {");
                        output.Add("            \"name\": \"Unity\",");
                        output.Add("            \"expression\": \"1.0.0\",");
                        output.Add("            \"define\": \"" + definition + "\"");
                        output.Add("        }");
                        output.Add("    ],");
                        status = 0;
                    }
                    else if (s.Contains("\"versionDefines\""))
                    {
                        insideDefines = true;
                        output.Add(s);
                    }
                    else
                        output.Add(s);
                }
                else
                {
                    if (s.Contains("],"))
                    {
                        insideDefines = false;
                        output.Add(s);
                    }
                    else
                    {
                        status = AddDefine(input, ref i, output, definition);
                        if (status == -2)//already exist!
                            return input;
                        if (status == 0)
                            insideDefines = false;
                    }
                }
            }
            return output;
        }

        private static int AddDefine(List<string> input, ref int index, List<string> output, string toAdd)
        {
            string bracket = input[index++];
            string name = input[index++];
            string expression = input[index++];
            string define = input[index++];
            string end = input[index];
            if (!bracket.Contains("{"))
            {
                //error
                return -1;
            }
            if (define.Contains(toAdd))
                return -2;

            if (!end.Contains(","))
            {//is last define
                output.Add(bracket);
                output.Add(name);
                output.Add(expression);
                output.Add(define);
                output.Add(end + ",");

                output.Add(bracket);
                output.Add("            \"name\": \"Unity\",");
                output.Add("            \"expression\": \"1.0.0\",");
                output.Add("            \"define\": \"" + toAdd + "\"");
                output.Add(end);
                return 0;
            }
            else
            {
                output.Add(bracket);
                output.Add(name);
                output.Add(expression);
                output.Add(define);
                output.Add(end);
            }
            return 1;
        }

        private static int RemoveDefine(List<string> input, ref int index, List<string> output, string toRemove)
        {
            string bracket = input[index++];
            string name = input[index++];
            string expression = input[index++];
            string define = input[index++];
            string end = input[index];
            if (!bracket.Contains("{"))
            {
                //error
                return -1;
            }
            if (define.Contains(toRemove))
            {
                if (!end.Contains(","))
                {//this was last element!
                    string before = input[index - 5];
                    if (before.Contains("},"))
                    {
                        if (output[output.Count - 1].Contains("},"))
                        {
                            output[output.Count - 1] = end;
                        }
                    }
                    else if (before.Contains("\"versionDefines\": ["))
                    {//was only!
                        if (output[output.Count - 1].Contains("\"versionDefines\": ["))
                        {
                            output[output.Count - 1] = "    \"versionDefines\": [],";
                            ++index;
                        }
                    }
                    return 0;
                }
                return 1;

            }
            else
            {
                output.Add(bracket);
                output.Add(name);
                output.Add(expression);
                output.Add(define);
                output.Add(end);

            }
            return 1;
        }



        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new InstancedAnimationSystemSettingsProvider("Project/Instanced Animation System", SettingsScope.Project);

            provider.keywords = GetSearchKeywordsFromGUIContentProperties<GUIContents>();
            return provider;
        }
    }
}