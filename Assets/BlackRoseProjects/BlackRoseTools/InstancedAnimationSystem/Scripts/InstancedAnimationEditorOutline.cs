#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [ExecuteAlways, HideInInspector]
    internal class InstancedAnimationEditorOutline : MonoBehaviour
    {
#if UNITY_EDITOR
        internal static List<InstancedAnimationEditorOutline> instances = new List<InstancedAnimationEditorOutline>();
        internal delegate void OnRender(CommandBuffer buffer);
        internal static OnRender onRenderOutline;

        internal InstancedAnimationSystemSettings settings;
        internal Material outlineMaterial;
        [SerializeField] internal UnityEditor.SceneView sceneView;
        internal bool wasActive;

        private readonly int selectionBuffer = Shader.PropertyToID("_SelectionBuffer");
        private readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");

        private void Awake()
        {
            instances.Add(this);
        }

        private void OnEnable()
        {
            if (outlineMaterial == null)
                outlineMaterial = new Material(Shader.Find(InstancedAnimationHelper.BuiltInEditorOutline));
        }

        private void OnDestroy()
        {
            instances.Remove(this);
            DestroyImmediate(outlineMaterial);
        }

        internal static void DestroyAll()
        {
            while (instances.Count > 0)
                DestroyImmediate(instances[instances.Count - 1]);
        }

        private void Update()
        {
            wasActive = false;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (settings == null)
                settings = InstancedAnimationSystemSettings.GetSettings();
            wasActive = true;
            if (settings.selectionOutlineActive)
            {
                var commands = new CommandBuffer();

                commands.GetTemporaryRT(selectionBuffer, source.descriptor);

                commands.SetRenderTarget(selectionBuffer);
                commands.ClearRenderTarget(true, true, Color.clear);

                if (onRenderOutline != null)
                    onRenderOutline.Invoke(commands);

                if (outlineMaterial == null)
                    outlineMaterial = new Material(Shader.Find(InstancedAnimationHelper.BuiltInEditorOutline));
               
                outlineMaterial.SetColor(_OutlineColor, settings.selectionOutlineColor);
                commands.Blit(source, destination, outlineMaterial);
                commands.ReleaseTemporaryRT(selectionBuffer);

                Graphics.ExecuteCommandBuffer(commands);
                commands.Release();
                Graphics.SetRenderTarget(destination);
            }else
                Graphics.Blit(source, destination);
        }
#endif
    }
}
#endif