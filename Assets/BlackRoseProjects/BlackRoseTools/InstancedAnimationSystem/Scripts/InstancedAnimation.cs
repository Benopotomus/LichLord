#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// Manager of Instanced Renderers. Allow to set runtime settings and create Transformless Instanced Renderers 
    /// </summary>
    public static class InstancedAnimation
    {
        /// <summary>
        /// Allow to switch between using instancing or not
        /// </summary>
        public static bool UseInstancing
        {
            get { return InstancedAnimationManager.Instance.useInstancing; }
            set { InstancedAnimationManager.Instance.useInstancing = value; }
        }

        /// <summary>
        /// Switch current camera for Instanced rendering. Camera determinate culling and calculating LOD
        /// </summary>
        public static Camera CurrentCamera
        {
            get { return InstancedAnimationManager.Instance.CurrentCamera; }
            set { InstancedAnimationManager.Instance.CurrentCamera = value; }
        }

        /// <summary>
        /// Force clear all buffers 
        /// </summary>
        public static void ClearBuffers()
        {
            InstancedAnimationManager.Instance.Clear();
        }

        /// <summary>
        /// Create Transformless Instanced Renderer without GameObject or any Unity dependence
        /// </summary>
        /// <param name="animationData">Animation data for creating Instanced Renderer</param>
        /// <returns>Instanced renderer if successfully created or null if unable create</returns>
        public static InstancedRenderer CreateInstancedRenderer(InstancedAnimationData animationData)
        {
            return InstancedAnimationManager.Instance.CreateInstancedRendererInstance(animationData, 0, false, InstancingCullingMode.AlwaysAnimate);
        }

        /// <summary>
        /// Create Transformless Instanced Renderer without GameObject or any Unity dependence
        /// </summary>
        /// <param name="animationData">Animation data for creating Instanced Renderer</param>
        /// <param name="defaultAnimation">Default animation that will be playing on start</param>
        /// <param name="applyRootMotion">Apply root motion for this InstancedRenderer if animation support it</param>
        /// <param name="cullingMode">Animation culling mode allow to pause animation while object is not visable</param>
        /// <returns>Instanced renderer if successfully created or null if unable create</returns>
        public static InstancedRenderer CreateInstancedRenderer(InstancedAnimationData animationData, int defaultAnimation = 0, bool applyRootMotion = false, InstancingCullingMode cullingMode = InstancingCullingMode.AlwaysAnimate)
        {
            return InstancedAnimationManager.Instance.CreateInstancedRendererInstance(animationData, defaultAnimation, applyRootMotion, cullingMode);
        }

        /// <summary>
        /// Create Transformless Instanced Renderer without GameObject or any Unity dependence
        /// </summary>
        /// <param name="animationData">Animation data for creating Instanced Renderer</param>
        /// <param name="bakedAnimator">Baked animator that will be initialized in created InstancedRenderer</param>
        /// <returns>Instanced renderer if successfully created or null if unable create</returns>
        public static InstancedRenderer CreateInstancedRenderer(InstancedAnimationData animationData, InstancedAnimatorData bakedAnimator)
        {
            return InstancedAnimationManager.Instance.CreateInstancedRendererInstance(animationData, bakedAnimator, false, InstancingCullingMode.AlwaysAnimate);
        }

        /// <summary>
        /// Create Transformless Instanced Renderer without GameObject or any Unity dependence
        /// </summary>
        /// <param name="AnimationData">Animation data for creating Instanced Renderer</param>
        /// <param name="bakedAnimator">Baked animator that will be initialized in created InstancedRenderer</param>
        /// <param name="applyRootMotion">Apply root motion for this InstancedRenderer if animation support it</param>
        /// <param name="cullingMode">Animation culling mode allow to pause animation while object is not visable</param>
        /// <returns>Instanced renderer if successfully created or null if unable create</returns>
        public static InstancedRenderer CreateInstancedRenderer(InstancedAnimationData AnimationData, InstancedAnimatorData bakedAnimator, bool applyRootMotion = false, InstancingCullingMode cullingMode = InstancingCullingMode.AlwaysAnimate)
        {
            return InstancedAnimationManager.Instance.CreateInstancedRendererInstance(AnimationData, bakedAnimator, applyRootMotion, cullingMode);
        }
    }
}
#endif