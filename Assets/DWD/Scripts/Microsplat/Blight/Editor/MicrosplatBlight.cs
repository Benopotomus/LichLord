using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Text;

namespace JBooth.MicroSplat
{
    public class MicrosplatBlight : FeatureDescriptor
    {
        const string sDefine = "__MICROSPLAT_BLIGHT__";
        static MicrosplatBlight()
        {
            MicroSplatDefines.InitDefine(sDefine);
        }
        [PostProcessSceneAttribute(0)]
        public static void OnPostprocessScene()
        {
            MicroSplatDefines.InitDefine(sDefine);
        }


        public bool blightEnabled = false;

        private static TextAsset blight_props;
        private static TextAsset blight_buffer;
        private static TextAsset blight_funcs;

        public override string ModuleName() { return "Blight"; }

        public override string GetVersion() { return "3.9"; } //this is the MS version compatability


        public override string GetHelpPath()
        {
            return "https://docs.google.com/document/d/1LAm2J2EDHArIxXuGJO8iIghjR8J3oNRhN4VPE0t_9bM/edit?usp=sharing";
        }

        public override void DrawFeatureGUI(MicroSplatKeywords keywords)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                blightEnabled = EditorGUILayout.ToggleLeft(new GUIContent("Enable Blight?"), blightEnabled);
            }
        }

        public override void DrawShaderGUI(MicroSplatShaderGUI shaderGUI, MicroSplatKeywords keywords, Material mat, MaterialEditor materialEditor, MaterialProperty[] props)
        {
            if(blightEnabled)
            {
                MaterialProperty blightTex = shaderGUI.FindProp("_BlightTex", props);
                MaterialProperty blightData = shaderGUI.FindProp("_BlightData", props);
                MaterialProperty blightCount = shaderGUI.FindProp("_BlightCount", props);
                MaterialProperty blightCutoff = shaderGUI.FindProp("_BlightCutoff", props);
                MaterialProperty blightPow = shaderGUI.FindProp("_BlightPow", props);
                MaterialProperty blightBoost = shaderGUI.FindProp("_BlightBoost", props);

                using(new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    materialEditor.ShaderProperty(blightTex, new GUIContent(blightTex.displayName));
                    materialEditor.ShaderProperty(blightData, new GUIContent(blightTex.displayName));
                    materialEditor.ShaderProperty(blightCount, new GUIContent(blightTex.displayName));
                    materialEditor.ShaderProperty(blightCutoff, new GUIContent(blightTex.displayName));
                    materialEditor.ShaderProperty(blightPow, new GUIContent(blightTex.displayName));
                    materialEditor.ShaderProperty(blightBoost, new GUIContent(blightTex.displayName));
                }
            }
        }

        public override void Unpack(string[] keywords)
        {
            int count = keywords.Length;
            for(int a = 0; a < count; a++)
            {
                string temp = keywords[a];
                if(temp == "_BLIGHT")
                {
                    blightEnabled = true;
                    break;
                }
            }
        }

        public override string[] Pack() 
        {
            List<string> output = new List<string>();

            if (blightEnabled)
                output.Add("_BLIGHT");
            return output.ToArray();
        }

        public override void InitCompiler(string[] paths)
        {
            int count = paths.Length;
            for(int a = 0; a < count; a++)
            {
                string path = paths[a];
                if (path.EndsWith("microsplat_properties_blight.txt"))
                    blight_props = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                else if (path.EndsWith("microsplat_func_blight.txt"))
                    blight_funcs = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                else if (path.EndsWith("microsplat_cbuffer_blight.txt"))
                    blight_buffer = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }
        }

        public override void WriteProperties(string[] features, StringBuilder sb)
        {
           if(blightEnabled)
                sb.AppendLine(blight_props.text);
        }

        public override void WritePerMaterialCBuffer(string[] features, StringBuilder sb)
        {
            if(blightEnabled)
                sb.AppendLine(blight_buffer.text);
        }

        public override void ComputeSampleCounts(string[] features, ref int arraySampleCount, ref int textureSampleCount, ref int maxSamples, ref int tessellationSamples, ref int depTexReadLevel)
        {
            if (blightEnabled)
                textureSampleCount += 2;
        }

        public override void WriteFunctions(string[] features, StringBuilder sb)
        {
            if (blightEnabled)
                sb.AppendLine(blight_funcs.text);
        }
    }
}