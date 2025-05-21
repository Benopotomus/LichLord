using UnityEngine;
using UnityEngine.Rendering;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BRPUtilities.Editor", AllInternalsVisible = true)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MeshUtility", AllInternalsVisible = true)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MeshUtility.Editor", AllInternalsVisible = false)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstancedAnimationSystem", AllInternalsVisible = true)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstancedAnimationSystem.Editor", AllInternalsVisible = true)]
namespace BlackRoseProjects.Utility
{
    internal enum Pipelines
    {
        Built_In = 0,
        URP = 1,
        HDRP = 2,
        Other = 3
    }

    internal static class BRPPipelineHelper
    {
        private const string URP_Type = "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";
        private const string HDRP_Type = "UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset";

        /// <summary>
        /// Get information about current RenderPipeline
        /// </summary>
        /// <returns>RenderPipeline that is currently in GraphicalSettings</returns>
        public static Pipelines GetCurrentPipeline()
        {
            RenderPipelineAsset currentAsset = QualitySettings.renderPipeline == null ? GraphicsSettings.defaultRenderPipeline : QualitySettings.renderPipeline;

            if (currentAsset == null)
                return Pipelines.Built_In;
            else
            {
                string currentType = currentAsset.GetType().ToString();
                if (currentType == URP_Type)
                    return Pipelines.URP;
                else if (currentType == HDRP_Type)
                    return Pipelines.HDRP;
                else
                    return Pipelines.Other;
            }
        }
    }
}