#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// Stores data about attachment and allow to manage it
    /// </summary>
    public class InstancedAnimationAttachment
    {
        internal RuntimeInstancingSharedAttachment shared;
        internal InstancedRenderer parent;
        internal bool render;

        internal InstancedAnimationAttachment(RuntimeInstancingSharedAttachment shared)
        {
            this.shared = shared;
            this.render = true;
        }

        /// <summary>
        /// Set attachment position, rotation and scale relative to attached bone. This will update data of all shared attachments!
        /// </summary>
        /// <param name="position">Position offset relative to bone</param>
        /// <param name="rotation">Rotation offset relative to bone</param>
        /// <param name="scale">Scale of attachment</param>
        public void Config(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (parent != null)
                parent.ConfigAttachment(this, position, rotation, scale);
        }

        /// <summary>
        /// Set attachment position, rotation and scale relative to attached bone. This will update data of all shared attachments!
        /// </summary>
        /// <param name="position">Position offset relative to bone</param>
        /// <param name="rotation">Rotation offset relative to bone</param>
        /// <param name="scale">Scale of attachment</param>
        public void Config(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            if (parent != null)
                parent.ConfigAttachment(this, position, rotation, scale);
        }

        /// <summary>
        /// Remove this attachment from Instanced Renderer
        /// </summary>
        public void Detach()
        {
            if (parent != null)
                parent.Deattach(this);
        }

        /// <summary>
        /// Allow to switch attachment rendering.
        /// </summary>
        public bool Enabled
        {
            get { return render; }
            set { render = value; }
        }

        /// <summary>
        /// Check if this Attachment is validly attached to Instanced Renderer
        /// </summary>
        public bool IsValid { get { return parent != null; } }

        /// <summary>
        /// InstancedAttachmentData that is shared for this Attachment
        /// </summary>
        public InstancedAttachmentData InstancedAttachmentData { get { return shared != null ? shared.attachmentData : null; } }

        /// <summary>
        /// Name of bone to which this attachment is hooked, or empty string when this attachment isn't attached
        /// </summary>
        public string BoneName { get { return shared != null ? shared.attachmentData.boneName : ""; } }

        /// <summary>
        /// Instanced renderer to which this attachment is hooked, or null when isn't attached
        /// </summary>
        public InstancedRenderer InstancedRenderer { get { return parent; } }
    }
}
#endif