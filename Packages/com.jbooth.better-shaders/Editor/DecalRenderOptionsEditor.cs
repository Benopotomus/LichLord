/////////////////////
// Better Shaders
// 
// You are free to distribute this file with your assets, 
// just namespace it to something else so that it doesn't conflict
/////////////////////

// This is required because Unity Graphics are a bunch of wankers who can't 
// decide if the shader controls things or the material editor does, and love
// to make everything internal so that you can't just use their stuff but
// instead have to roll your own versions of everything.
// 
// Without this, decals won't have the right flags set on them, etc.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if USING_HDRP
using UnityEditor.Rendering.HighDefinition;
#endif

namespace JBooth.BetterShaders
{
   public class DecalRenderOptionsEditor : SubShaderMaterialEditor
   {
        enum DepthBiasType
        {
            DepthBias = 0, ViewBias
        }


        public override void OnGUI(MaterialEditor materialEditor,
         ShaderGUI shaderGUI,
         MaterialProperty[] props,
         Material mat)
      {
#if USING_URP
            if (DrawRollout("Draw Flags"))
            {
                EditorGUI.indentLevel++;
                // we cannot use material properties here because
                // they don't exist as part of the block system but
                // rather as part of the template
                bool albedo = mat.GetFloat("_AffectAlbedo") > 0.5f ? true : false;
                bool normal = mat.GetFloat("_AffectNormal") > 0.5f ? true : false;
                bool normalBlend = mat.GetFloat("_AffectNormalBlend") > 0.5f ? true : false;
                bool maos = mat.GetFloat("_AffectMaos") > 0.5f ? true : false;

                EditorGUI.BeginChangeCheck();
                albedo = EditorGUILayout.Toggle("Affect Albedo", albedo);
                normal = EditorGUILayout.Toggle("Affect Normal", normal);
                normalBlend = EditorGUILayout.Toggle("Affect Normal Blend", normalBlend);
                maos = EditorGUILayout.Toggle("Affect MAOS", maos);
                if (EditorGUI.EndChangeCheck())
                {
                    mat.SetFloat("_AffectAlbedo", albedo ? 1 : 0);
                    mat.SetFloat("_AffectNormal", normal ? 1 : 0);
                    mat.SetFloat("_AffectNormalBlend", normalBlend ? 1 : 0);
                    mat.SetFloat("_AffectMaos", maos ? 1 : 0);

                    EditorUtility.SetDirty(mat);
                }

                int drawOrder = (int)mat.GetFloat("_DrawOrder");
                DepthBiasType decalMeshBiasType = (DepthBiasType)mat.GetFloat("_DecalMeshBiasType");
                float depthbias = mat.GetFloat("_DecalMeshDepthBias");
                float viewbias = mat.GetFloat("_DecalMeshViewBias");
                // fucking assholes with their internal unusable shit
                // min/max from HDRenderQueue, which is internal, because
                // why would anyone need to know that from external code?
                drawOrder = EditorGUILayout.IntSlider("Draw Order", drawOrder, -50, 50);
                decalMeshBiasType = (DepthBiasType)EditorGUILayout.EnumPopup("Bias Type", (DepthBiasType)decalMeshBiasType);

                if (decalMeshBiasType == (int)DepthBiasType.DepthBias)
                {
                    depthbias = EditorGUILayout.FloatField("Depth Bias", depthbias);
                }
                else if (decalMeshBiasType == DepthBiasType.ViewBias)
                {
                    viewbias = EditorGUILayout.FloatField("View Bias", viewbias);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    mat.SetFloat("_DrawOrder", drawOrder);
                    mat.SetFloat("_DecalMeshBiasType", decalMeshBiasType == DepthBiasType.DepthBias ? 0 : 1); ;
                    mat.SetFloat("_DecalMeshDepthBias", depthbias);
                    mat.SetFloat("_DecalMeshViewBias", viewbias);
                    mat.renderQueue = 2000 + drawOrder;
                    EditorUtility.SetDirty(mat);
                }
                EditorGUI.indentLevel--;
            }
#endif

#if USING_HDRP

            if (DrawRollout("Draw Flags"))
            {
                EditorGUI.indentLevel++;
                bool perChannelMask = false;
                var curPipe = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                if (curPipe is UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset)
                {
                   UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset hdrp = curPipe as UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset;
                   if (hdrp != null)
                   {
                      perChannelMask = hdrp.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;
                   }
                }

                // we cannot use material properties here because
                // they don't exist as part of the block system but
                // rather as part of the template
                bool albedo = mat.GetFloat("_AffectAlbedo") > 0.5f ? true : false;
                bool normal = mat.GetFloat("_AffectNormal") > 0.5f ? true : false;
                bool metal = mat.GetFloat("_AffectMetal") > 0.5f ? true : false;
                bool smoothness = mat.GetFloat("_AffectSmoothness") > 0.5f ? true : false;
                bool occlusion = mat.GetFloat("_AffectOcclusion") > 0.5f ? true : false;
                bool emission = mat.GetFloat("_AffectEmission") > 0.5f ? true : false;

                EditorGUI.BeginChangeCheck();
                albedo = EditorGUILayout.Toggle("Affect Albedo", albedo);
                normal = EditorGUILayout.Toggle("Affect Normal", normal);
                using (new EditorGUI.DisabledScope(!perChannelMask))
                {
                   metal = EditorGUILayout.Toggle("Affect Metal", metal);
                   occlusion = EditorGUILayout.Toggle("Affect Occlusion", occlusion);
                }
                smoothness = EditorGUILayout.Toggle("Affect Smoothness", smoothness);
                emission = EditorGUILayout.Toggle("Affect Emission", emission);
                if (EditorGUI.EndChangeCheck())
                {
                   mat.SetFloat("_AffectAlbedo", albedo ? 1 : 0);
                   mat.SetFloat("_AffectNormal", normal ? 1 : 0);
                   mat.SetFloat("_AffectSmoothness", smoothness ? 1 : 0);
                   mat.SetFloat("_AffectOcclusion", occlusion ? 1 : 0);
                   mat.SetFloat("_AffectMetal", metal ? 1 : 0);
                   mat.SetFloat("_AffectEmisison", emission ? 1 : 0);
                   EditorUtility.SetDirty(mat);
                }


                if (!perChannelMask && (mat.HasProperty("_AffectMetal") || mat.HasProperty("_AffectOcclusion")))
                {
                   EditorGUILayout.HelpBox("Enable 'Metal and AO properties' in your HDRP Asset if you want to control the Metal and AO properties of decals. There is a performance cost of enabling this option.",
                       MessageType.Info);
                }

                int drawOrder = (int)mat.GetFloat("_DrawOrder");
                DepthBiasType decalMeshBiasType = (DepthBiasType)mat.GetFloat("_DecalMeshBiasType");
                float depthbias = mat.GetFloat("_DecalMeshDepthBias");
                float viewbias = mat.GetFloat("_DecalMeshViewBias");
                // fucking assholes with their internal unusable shit
                // min/max from HDRenderQueue, which is internal, because
                // why would anyone need to know that from external code?
                drawOrder = EditorGUILayout.IntSlider("Draw Order", drawOrder, -50, 50);
                decalMeshBiasType = (DepthBiasType)EditorGUILayout.EnumPopup("Bias Type", (DepthBiasType)decalMeshBiasType);

                if (decalMeshBiasType == (int)DepthBiasType.DepthBias)
                {
                   depthbias = EditorGUILayout.FloatField("Depth Bias", depthbias);
                }
                else if (decalMeshBiasType == DepthBiasType.ViewBias)
                {
                   viewbias = EditorGUILayout.FloatField("View Bias", viewbias);
                }

                if (EditorGUI.EndChangeCheck())
                {
                   mat.SetFloat("_DrawOrder", drawOrder);
                   mat.SetFloat("_DecalMeshBiasType", decalMeshBiasType == DepthBiasType.DepthBias ? 0 : 1); ;
                   mat.SetFloat("_DecalMeshDepthBias", depthbias);
                   mat.SetFloat("_DecalMeshViewBias", viewbias);
                   EditorUtility.SetDirty(mat);
                }
                 EditorGUI.indentLevel--;
            }

            //UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(mat);

#endif


        }
    }
}
