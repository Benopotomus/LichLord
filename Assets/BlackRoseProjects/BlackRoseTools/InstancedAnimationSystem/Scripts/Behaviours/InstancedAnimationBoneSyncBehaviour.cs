#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// This script is example of how add and manage bone synchronization for InstancedAnimationRenderer.
    /// <br/>Automatically start synchronization of given transform for bone of given name at Start, remove synchronization at OnDestroy.
    /// <br/>Pause synchronization at OnDisable and resume synchronization at OnEnable
    /// </summary>
    [DefaultExecutionOrder(200), RequireComponent(typeof(InstancedAnimationRenderer))]//execute after InstancedAnimatorRenderer
    [AddComponentMenu("Black Rose Projects/Instanced Animation System/Instanced Bone Sync Behaviour", 0)]
    [Icon("Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Icons/Icon_BoneSync.png")]
    [HelpURL("http://docs.blackrosetools.com/InstancedAnimations/html/class_black_rose_projects_1_1_instanced_animation_system_1_1_instanced_animation_bone_sync_behaviour.html")]
    public class InstancedAnimationBoneSyncBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Transform that will be synchronized by Instanced Animation Renderer
        /// </summary>
        [Tooltip("Thansform that will be synchronized to animation bone")] 
        public Transform transformToSync;

        /// <summary>
        /// Name of bone which will be synchronized
        /// </summary>
        [Tooltip("Name of bone in animation rig")] public string boneName;
        [SerializeField] private InstancedAnimationRenderer targetRenderer;

        #region Unity Calls
        private void Reset()
        {
            targetRenderer = GetComponent<InstancedAnimationRenderer>();
        }
#if UNITY_EDITOR
        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<InstancedAnimationRenderer>();
            if (targetRenderer != null && string.IsNullOrEmpty(boneName) && targetRenderer.animationData != null)
                boneName = targetRenderer.animationData.bonesNames[0];
        }
#endif

        private void Start()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<InstancedAnimationRenderer>();
            if (transformToSync != null && !string.IsNullOrEmpty(boneName))
                StartSynchronization(transformToSync, boneName);
        }

        private void OnDestroy()
        {
            StopSynchronization();
        }

        private void OnEnable()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<InstancedAnimationRenderer>();
            if (targetRenderer != null && targetRenderer.InstancedRenderer != null)
                targetRenderer.InstancedRenderer.SetBoneSyncPause(boneName, false);
        }

        private void OnDisable()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<InstancedAnimationRenderer>();
            if (targetRenderer != null && targetRenderer.InstancedRenderer != null)
                targetRenderer.InstancedRenderer.SetBoneSyncPause(boneName, true);
        }
        #endregion

        /// <summary>
        /// Start synchronization of given Transform with given bone name on Instanced Animation Renderer.
        /// <br/>Transform will be synchronized by position and rotation with scale set at 1,1,1
        /// </summary>
        /// <param name="transform">Transform that will be synchronized. Cannot be null. Also set this object boneTransform to this value</param>
        /// <param name="boneName">Name of bone to attach synchronization. Bone must match any non synchronizad yet bone from Instanced Animation Renderer. Also set this object boneName to this value</param>
        /// <returns>True if synchronization started, false if not</returns>
        public bool StartSynchronization(Transform transform, string boneName)
        {
            if (targetRenderer != null && targetRenderer.InstancedRenderer != null)
            {
                this.boneName = boneName;
                this.transformToSync = transform;
                return targetRenderer.InstancedRenderer.StartBoneSyncTransform(transform, boneName);
            }
            return false;
        }

        /// <summary>
        /// Start synchronization with parameters from this script boneTransform and boneName
        /// <br/>Transform will be synchronized by position and rotation with scale set at 1,1,1
        /// </summary>
        /// <returns>True if synchronization started, false if not</returns>
        public bool StartSynchronization()
        {
            if (targetRenderer != null && targetRenderer.InstancedRenderer != null && transformToSync != null)
                return targetRenderer.InstancedRenderer.StartBoneSyncTransform(transformToSync, boneName);
            return false;
        }

        /// <summary>
        /// Stop synchronization of current Instanced Animation Renderer with bone name set on this script
        /// </summary>
        /// <returns>True if synchronization was stopped, false if not or was already stoped</returns>
        public bool StopSynchronization()
        {
            if (targetRenderer != null && targetRenderer.InstancedRenderer != null)
                return targetRenderer.InstancedRenderer.StopBoneSync(boneName);
            return false;
        }
    }
}
#endif