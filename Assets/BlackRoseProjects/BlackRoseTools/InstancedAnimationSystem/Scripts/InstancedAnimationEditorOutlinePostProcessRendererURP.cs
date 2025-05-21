#if BLACKROSE_INSTANCING_URP && BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine.Rendering.Universal;

namespace BlackRoseProjects.InstancedAnimationSystem
{

    [System.Serializable]
    internal class InstancedAnimationEditorOutlinePostProcessRendererURP : ScriptableRendererFeature
    {
        InstancedAnimationEditorOutlinePassURP pass;

        public override void Create()
        {
#if UNITY_EDITOR
            pass = new InstancedAnimationEditorOutlinePassURP();
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            renderer.EnqueuePass(pass);
#endif
        }
    }
}
#endif