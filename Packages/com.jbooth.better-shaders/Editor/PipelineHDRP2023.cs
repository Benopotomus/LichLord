using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JBooth.BetterShaders
{
   public class PipelineHDRP2023 : IPipelineAdapter
   {
      public StringBuilder GetTemplate(Options options, ShaderBuilder.RenderPipeline renderPipeline, BetterShaderUtility util, ref StringBuilder defines)
      {
         StringBuilder template = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023.txt"));

         var passGBuffer = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassGBuffer.txt"));
         var passShadow = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassShadow.txt"));
         var passDepthOnly = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassDepthOnly.txt"));
         var passDepthForwardOnly = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassDepthForwardOnly.txt"));

         var passForward = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassForward.txt"));
         var passMeta = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassMeta.txt"));
         var passSceneSelect = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassSceneSelection.txt"));
         var vert = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_Vert.txt"));
         var hdrpShared = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_shared.txt"));
         var hdrpInclude = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_include.txt"));

         var passMotion = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassMotionVector.txt"));
         var passPicking = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassPicking.txt"));
         var passTransparentDepth = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassTransparentDepthPrepass.txt"));
         var passFullDebug = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassFullScreenDebug.txt"));
         var passForwardUnlit = new StringBuilder(util.LoadTemplate("BetterShaders_Template_HDRP2023_PassForwardUnlit.txt"));


        if (options.enableTransparentDepthPrepass == Options.Bool.False)
        {
            passTransparentDepth.Length = 0;
        }
        if (options.disableShadowCasting == Options.Bool.True)
        {
            passShadow.Clear();
        }
        if (options.disableGBuffer == Options.Bool.True)
        {
            passGBuffer.Clear();
        }
        if (options.workflow == Options.Workflow.Unlit)
        {
            passForward = passForwardUnlit;
            passGBuffer.Clear();
        }
        else
        {
            passDepthForwardOnly.Clear();
        }

        if (options.alpha != Options.AlphaModes.Opaque)
        {
            passShadow.Clear();
            passDepthOnly.Clear();
            passGBuffer.Clear();
            if (options.alpha == Options.AlphaModes.PreMultiply)
            {
                defines.AppendLine("#define _BLENDMODE_PRE_MULTIPLY 1");
                passForward = passForward.Replace("%FORWARDBASEBLEND%", "Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha\nCull Back\n ZTest LEqual\nZWrite Off");
            }
            else if (options.alpha == Options.AlphaModes.Add)
            {
                defines.AppendLine("#define _BLENDMODE_ADD 1");
                passForward = passForward.Replace("%FORWARDBASEBLEND%", "Blend One One, One OneMinusSrcAlpha\nCull Back\n ZTest LEqual\nZWrite Off");
            }
            else
            {
                defines.AppendLine("#define _BLENDMODE_ALPHA 1");
                passForward = passForward.Replace("%FORWARDBASEBLEND%", "Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha\nCull Back\n ZTest LEqual\nZWrite Off");
            }
            defines.AppendLine("#define _SURFACE_TYPE_TRANSPARENT 1");

        }
        else
        {
            passForward = passForward.Replace("%FORWARDBASEBLEND%", "");
        }
        

        template = template.Replace("%PASSFORWARD%", passForward.ToString());
        template = template.Replace("%PASSSHADOW%", passShadow.ToString());
        template = template.Replace("%PASSGBUFFER%", passGBuffer.ToString());
        template = template.Replace("%PASSDEPTHONLY%", passDepthOnly.ToString());
        template = template.Replace("%PASSDEPTHFORWARDONLY%", passDepthForwardOnly.ToString());

        template = template.Replace("%PASSMETA%", passMeta.ToString());
        template = template.Replace("%PASSSCENESELECT%", passSceneSelect.ToString());
        template = template.Replace("%PASSMOTIONVECTOR%", passMotion.ToString());
        template = template.Replace("%PASSSCENEPICKING%", passPicking.ToString());
        template = template.Replace("%PASSTRANSPARENTDEPTHPREPASS%", passTransparentDepth.ToString());
        template = template.Replace("%PASSFULLSCREENDEBUG%", passFullDebug.ToString());
        template = template.Replace("%VERT%", vert.ToString());

     
         template = template.Replace("%HDRPSHARED%", hdrpShared.ToString());
         template = template.Replace("%HDRPINCLUDE%", hdrpInclude.ToString());

         
         // HDRP tags are different, blerg..
         string tagString = "";
         if (options.tags != null)
         {
            tagString = options.tags;
            tagString = "\"RenderPipeline\" = \"HDRenderPipeline\" " + tagString;
            tagString = tagString.Replace("Opaque", "HDLitShader");
         }
         else
         {
            tagString = "\"RenderPipeline\" = \"HDRenderPipeline\" \"RenderType\" = \"HDLitShader\" \"Queue\" = \"Geometry+225\"";
         }

         if (options.alpha != Options.AlphaModes.Opaque)
         {
            tagString = tagString.Replace("Geometry+225", "Transparent");
         }

         template = template.Replace("%TAGS%", tagString);
         template.Replace("%SUBSHADERTAGS%", "");
         return template;
      }
   }
}
