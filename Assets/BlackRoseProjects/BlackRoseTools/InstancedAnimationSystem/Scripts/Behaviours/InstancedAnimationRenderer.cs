#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;
using System.Collections.Generic;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// MonoBehaviour wrap for InstancedRenderer. Allow managing Instanced Renderer in scene hierarchy and inspector window
    /// </summary>
    [DisallowMultipleComponent, ExecuteAlways, DefaultExecutionOrder(-1000)]
    [AddComponentMenu("Black Rose Projects/Instanced Animation System/Instanced Animation Renderer", 0)]
    [Icon("Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Icons/Icon_InstancedAnimationRendererToggle.png")]
    [HelpURL("http://docs.blackrosetools.com/InstancedAnimations/html/class_black_rose_projects_1_1_instanced_animation_system_1_1_instanced_animation_renderer.html")]
    public sealed class InstancedAnimationRenderer : MonoBehaviour
    {
        [SerializeField] internal InstancedAnimationData animationData;
        [SerializeField] internal InstancedAnimatorData animator;
        [SerializeField] internal InstancingCullingMode cullingMode = InstancingCullingMode.AlwaysAnimate;
        [SerializeField, DefaultAnimationIndex] internal int defaultAnimation = 0;
        [SerializeField] internal bool applyRootMotion = false;

        [System.NonSerialized] internal InstancedRenderer rendererInstance;

        #region EditorOnly
#if UNITY_EDITOR
        [SerializeField, HideInInspector] private InstancedAnimationData editor_lastUsedData;
        [SerializeField, HideInInspector] internal bool editor_autoPlayMode;
        [SerializeField, HideInInspector] internal int editor_selectedAnimation;
        [SerializeField, HideInInspector] internal int editor_previousAnimation;
        [SerializeField, HideInInspector] internal float editor_selectedAnimationFrame;
        [SerializeField, HideInInspector] internal float editor_previousAnimationFrame;
        [SerializeField, HideInInspector] internal float editor_playbackSpeed = 1;
        [SerializeField, HideInInspector] internal static List<InstancedAnimationRenderer> editor_behaviours = new List<InstancedAnimationRenderer>();
#endif
        #endregion

        /// <summary>
        /// Instanced Renderer that is using by this Behaviour
        /// </summary>
        public InstancedRenderer InstancedRenderer { get { return rendererInstance; } }

        /// <summary>
        /// Baked Animation data attached to this renderer
        /// </summary>
        public InstancedAnimationData AnimationData { get { return rendererInstance != null ? rendererInstance.animationData : animationData; } }

        private void OnEnable()
        {
            if (rendererInstance != null)
                rendererInstance.isHidden = false;
            #region EditorOnly
#if UNITY_EDITOR
            editor_behaviours.Add(this);
#endif
            #endregion
        }

        private void OnDisable()
        {
            if (rendererInstance != null)
                rendererInstance.isHidden = true;
            #region EditorOnly
#if UNITY_EDITOR
            editor_behaviours.Remove(this);
#endif
            #endregion
        }

        internal void Start()
        {
            #region EditorOnly
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                rendererInstance = new InstancedRenderer(animationData, animator, defaultAnimation, applyRootMotion, cullingMode);
                rendererInstance.Initialize(transform);
                rendererInstance.EditorOnly_RefreshMaterials();
            }
            return;
#endif
#pragma warning disable CS0162
            #endregion
            rendererInstance = new InstancedRenderer(animationData, animator, defaultAnimation, applyRootMotion, cullingMode);
            rendererInstance.Initialize(transform);
            #region EditorOnly
#if UNITY_EDITOR
#pragma warning restore CS0162
#endif
            #endregion
        }

        private void OnDestroy()
        {
            #region EditorOnly
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                #endregion
                if (!InstancedAnimationManager.IsDestroyed && rendererInstance != null)
                {
                    InstancedAnimationManager.Instance.RemoveInstance(rendererInstance);
                    rendererInstance = null;
                }
                #region EditorOnly
#if UNITY_EDITOR
            }
            else
            {
                if (rendererInstance != null)
                    rendererInstance = null;
            }
#endif
            #endregion
        }
        #region EditorOnly
#if UNITY_EDITOR
        internal void EditorOnly_Update(List<Camera> cameras, Camera lodCamera, float deltaTime)
        {
            if (animationData == null)
            {
                if (rendererInstance != null)
                    rendererInstance = null;
            }
            else
            {
                if (rendererInstance == null)
                {
                    rendererInstance = new InstancedRenderer(animationData, animator, defaultAnimation, applyRootMotion, cullingMode);
                    rendererInstance.EditorOnly_Initialize(transform);
                    editor_lastUsedData = animationData;
                }
                else if (editor_lastUsedData != animationData /*|| UnityEditor.EditorUtility.IsDirty(animationData)*/)
                {
                    rendererInstance = new InstancedRenderer(animationData, animator, defaultAnimation, applyRootMotion, cullingMode);
                    rendererInstance.EditorOnly_Initialize(transform);
                    editor_lastUsedData = animationData;
                }
                if (editor_autoPlayMode)
                {
                    editor_selectedAnimationFrame += animationData.animations[editor_selectedAnimation].fps * deltaTime * editor_playbackSpeed;
                    if (editor_selectedAnimationFrame > animationData.animations[editor_selectedAnimation].totalFrame - 1)
                        editor_selectedAnimationFrame -= animationData.animations[editor_selectedAnimation].totalFrame - 1;
                    else if (editor_selectedAnimationFrame < 0)
                        editor_selectedAnimationFrame += animationData.animations[editor_selectedAnimation].totalFrame - 1;

                    editor_previousAnimationFrame += animationData.animations[editor_previousAnimation].fps * deltaTime * editor_playbackSpeed;
                    if (editor_previousAnimationFrame > animationData.animations[editor_previousAnimation].totalFrame - 1)
                        editor_previousAnimationFrame -= animationData.animations[editor_previousAnimation].totalFrame - 1;
                    else if (editor_previousAnimationFrame < 0)
                        editor_previousAnimationFrame += animationData.animations[editor_previousAnimation].totalFrame - 1;

                }
                rendererInstance.editor_frameIndex = editor_selectedAnimationFrame + animationData.animations[editor_selectedAnimation].animationIndex;
                rendererInstance.editor_preFrameIndex = editor_previousAnimationFrame + animationData.animations[editor_previousAnimation].animationIndex;
                rendererInstance.editor_currentLODLevel = EditorOnly_CalcLodLevels(lodCamera);
                rendererInstance.EditorOnly_Render(cameras);
                if (InstancedRenderer.editor_transitionUpdate && rendererInstance.editor_transition < 1f)
                {
                    rendererInstance.editor_transition += deltaTime / InstancedRenderer.editor_transitionScale * editor_playbackSpeed;
                    if (rendererInstance.editor_transition > 1f)
                        rendererInstance.editor_transition = 1f;
                }
            }
        }

        internal void EditorOnly_RenderOutline(UnityEngine.Rendering.CommandBuffer cb)
        {
            if (rendererInstance != null && enabled && gameObject.activeInHierarchy)
                rendererInstance.EditorOnly_RenderOutline(cb);
        }

        internal void EditorOnly_RenderOutline(List<Camera> camera, Material material)
        {
            if (rendererInstance != null && enabled && gameObject.activeInHierarchy)
                rendererInstance.EditorOnly_Render(camera, material);
        }

        internal void EditorOnly_RenderOutlinePlayMode(UnityEngine.Rendering.CommandBuffer cb)
        {
            if (rendererInstance != null && enabled && gameObject.activeInHierarchy)
                rendererInstance.EditorOnly_RenderOutlinePlayMode(cb);
        }

        internal int EditorOnly_CalcLodLevels(Camera cam)
        {
            float distance = (cam.transform.position - transform.position).sqrMagnitude;
            float scale = transform.lossyScale.FlatScale();
            float bias = QualitySettings.lodBias;
            distance /= bias;
            int editor_lodlevel = -1;
            for (int i = 0; i < animationData.LOD.Length; ++i)
            {
                InstancingLODData lod = animationData.LOD[i];
                float l = lod.height * scale;
                l *= l;
                if (l > distance)
                {
                    editor_lodlevel = i;
                    break;
                }
            }
            if (animationData.LOD.Length == 1 && animationData.LOD[0].height == -1)
                editor_lodlevel = 0;
            return editor_lodlevel;
        }
#endif
        #endregion
    }
}
#endif