#if BLACKROSE_INSTANCING_URP && BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace BlackRoseProjects.InstancedAnimationSystem
{

    [Serializable, VolumeComponentMenuForRenderPipeline("Black Rose Projects/URP/URP editor instancing outline", typeof(UniversalRenderPipeline))]
    internal class InstancedAnimationEditorOutlineVolumeEffectURP : VolumeComponent, IPostProcessComponent
    {
        public bool IsActive()
        {
#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
        public bool IsTileCompatible() => true;
    }

}
#endif