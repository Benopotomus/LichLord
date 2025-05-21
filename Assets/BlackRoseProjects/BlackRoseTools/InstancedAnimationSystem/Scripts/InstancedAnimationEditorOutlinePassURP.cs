#if BLACKROSE_INSTANCING_URP && BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [System.Serializable]
    internal class InstancedAnimationEditorOutlinePassURP : ScriptableRenderPass
    {
#if UNITY_2022_3_OR_NEWER
        [System.NonSerialized] RTHandle source;
        [System.NonSerialized] RTHandle selectionBufferIdentifier2;
        [System.NonSerialized] RTHandleSystem m_RTHandleSystem = new RTHandleSystem();
#else
        [System.NonSerialized] RenderTargetIdentifier source;
        [System.NonSerialized] RenderTargetIdentifier selectionBufferIdentifier2;
#endif
        [System.NonSerialized] private readonly int selectionBuffer = Shader.PropertyToID("_SelectionBuffer");
        [System.NonSerialized] private readonly int mainText2 = Shader.PropertyToID("_MainTex2");
        [System.NonSerialized] private readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
        [System.NonSerialized] internal Material outlineMaterial;
        [System.NonSerialized] internal InstancedAnimationSystemSettings settings;

        public InstancedAnimationEditorOutlinePassURP()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            var renderer = renderingData.cameraData.renderer;
#if UNITY_2022_3_OR_NEWER
            source = renderer.cameraColorTargetHandle;
#else
            source = renderer.cameraColorTarget;
#endif

            cmd.GetTemporaryRT(selectionBuffer, descriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(mainText2, descriptor, FilterMode.Bilinear);
#if UNITY_2022_3_OR_NEWER
            m_RTHandleSystem = new RTHandleSystem();
            selectionBufferIdentifier2 = m_RTHandleSystem.Alloc(new RenderTargetIdentifier(mainText2));
#else
            selectionBufferIdentifier2 = new RenderTargetIdentifier(mainText2);
#endif
#endif
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (settings == null)
                settings = InstancedAnimationSystemSettings.GetSettings();
            if (!renderingData.cameraData.isSceneViewCamera || !settings.selectionOutlineActive)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("Custom Post Processing");
            cmd.Clear();

            var stack = VolumeManager.instance.stack;
            var customEffect = stack.GetComponent<InstancedAnimationEditorOutlineVolumeEffectURP>();
            if (customEffect.IsActive())
            {

                Blit(cmd, source, selectionBufferIdentifier2);
                cmd.SetRenderTarget(selectionBuffer);
                cmd.ClearRenderTarget(true, true, Color.clear);

                if (InstancedAnimationEditorOutline.onRenderOutline != null)
                    InstancedAnimationEditorOutline.onRenderOutline.Invoke(cmd);

                if (outlineMaterial == null)
                    outlineMaterial = new Material(Shader.Find("Hidden/BlackRoseProjects/InstancedAnimationSystem/URP/CustomEditorOutline"));

                outlineMaterial.SetColor(_OutlineColor, settings.selectionOutlineColor);
                cmd.Blit(source, source, outlineMaterial, 0);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
#endif
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
#if UNITY_EDITOR
            cmd.ReleaseTemporaryRT(selectionBuffer);
            cmd.ReleaseTemporaryRT(mainText2);

#if UNITY_2022_3_OR_NEWER
            m_RTHandleSystem.Release(selectionBufferIdentifier2);
#endif
#endif
        }
    }
}
#endif