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

        public override string ModuleName() { return "Blight"; }

        public override string GetVersion() { return "1.0"; }


        public override string GetHelpPath()
        {
            return "https://docs.google.com/document/d/1LAm2J2EDHArIxXuGJO8iIghjR8J3oNRhN4VPE0t_9bM/edit?usp=sharing";
        }

        public override void DrawFeatureGUI(MicroSplatKeywords keywords)
        {
            throw new System.NotImplementedException();
        }

        public override void DrawShaderGUI(MicroSplatShaderGUI shaderGUI, MicroSplatKeywords keywords, Material mat, MaterialEditor materialEditor, MaterialProperty[] props)
        {
            throw new System.NotImplementedException();
        }

        public override void Unpack(string[] keywords)
        {
            throw new System.NotImplementedException();
        }

        public override string[] Pack() 
        {
            throw new System.NotImplementedException();
        }

        public override void InitCompiler(string[] paths)
        {
            throw new System.NotImplementedException();
        }

        public override void WriteProperties(string[] features, StringBuilder sb)
        {
            throw new System.NotImplementedException();
        }

        public override void WritePerMaterialCBuffer(string[] features, StringBuilder sb)
        {
            base.WritePerMaterialCBuffer(features, sb);
        }

        public override void ComputeSampleCounts(string[] features, ref int arraySampleCount, ref int textureSampleCount, ref int maxSamples, ref int tessellationSamples, ref int depTexReadLevel)
        {
            throw new System.NotImplementedException();
        }

        public override void WriteFunctions(string[] features, StringBuilder sb)
        {
            throw new System.NotImplementedException();
        }
    }
}