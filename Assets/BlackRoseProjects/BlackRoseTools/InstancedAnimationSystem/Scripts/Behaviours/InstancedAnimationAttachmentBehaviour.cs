#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// This script is example of how add and manage Attachments on InstancedAnimationRenderer.
    /// <br/>Attachments works only in playmode.
    /// <br/>To configurate attachment use Configurator Helper.
    /// <br/>Automatically create attachment at Start and remove it at OnDestroy.
    /// <br/>Hide attachment at OnDisable and show it at OnEnable
    /// </summary>
    [DefaultExecutionOrder(200), RequireComponent(typeof(InstancedAnimationRenderer))]//execute after InstancedAnimatorRenderer
    [AddComponentMenu("Black Rose Projects/Instanced Animation System/Instanced Attachment Behaviour", 0)]
    [Icon("Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Icons/Icon_AttachmentBehaviour.png")]
    [HelpURL("http://docs.blackrosetools.com/InstancedAnimations/html/class_black_rose_projects_1_1_instanced_animation_system_1_1_instanced_animation_attachment_behaviour.html")]
    public class InstancedAnimationAttachmentBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Attachment data containing informations about attachment
        /// </summary>
        [Tooltip("Attachment data containing info about attachment")]
        public InstancedAttachmentData attachementData;
        [SerializeField, HideInInspector] private InstancedAnimationRenderer targetRenderer;
        /// <summary>
        /// Created instance of attachment that has been created on runtime
        /// </summary>
        public InstancedAnimationAttachment animationAttachmentInstance;

        #region Unity Calls
        private void Reset()
        {
            targetRenderer = GetComponent<InstancedAnimationRenderer>();
        }

        private void Start()
        {
            if (attachementData != null)
                animationAttachmentInstance = Attach(attachementData);
        }

        private void OnDestroy()
        {
            Detach(animationAttachmentInstance);
            animationAttachmentInstance = null;
        }

        private void OnEnable()
        {
            if (animationAttachmentInstance != null)
                animationAttachmentInstance.Enabled = true;
            else if (attachementData != null)
            {
                if (targetRenderer == null)
                {
                    targetRenderer = GetComponent<InstancedAnimationRenderer>();
                    animationAttachmentInstance = Attach(attachementData);
                }
                else if (targetRenderer.InstancedRenderer != null)
                    animationAttachmentInstance = Attach(attachementData);
            }
        }

        private void OnDisable()
        {
            if (animationAttachmentInstance != null)
                animationAttachmentInstance.Enabled = false;
        }
        #endregion
        internal void AttachInternal(InstancedAttachmentData attachmentData)
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<InstancedAnimationRenderer>();
            if (targetRenderer != null && targetRenderer.InstancedRenderer != null && attachmentData != null)
                animationAttachmentInstance = targetRenderer.InstancedRenderer.Attach(attachmentData);
        }

        /// <summary>
        /// Attach given attachment data into current InstancedAnimationRenderer 
        /// </summary>
        /// <param name="attachmentData">Attachment data that store config to generate attachment</param>
        /// <returns>InstancedAnimationAttachment that allow to manage created attachment, or null if unable to create attachment</returns>
        public InstancedAnimationAttachment Attach(InstancedAttachmentData attachmentData)
        {
            if (targetRenderer != null && targetRenderer.InstancedRenderer != null)
                return targetRenderer.InstancedRenderer.Attach(attachmentData);
            return null;
        }

        /// <summary>
        /// Detach given attachment from curretn InstancedAnimationRenderer
        /// </summary>
        /// <param name="instancedAttachment">Attachment to detach</param>
        public void Detach(InstancedAnimationAttachment instancedAttachment)
        {
            if (instancedAttachment != null)
                instancedAttachment.Detach();
        }
    }
}
#endif