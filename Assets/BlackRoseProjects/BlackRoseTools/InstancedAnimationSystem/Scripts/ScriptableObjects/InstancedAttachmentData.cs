#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// Configuration data used to create attachment
    /// </summary>
    [Serializable, CreateAssetMenu(fileName = "InstancedAttachmentData", menuName = "Black Rose Projects/Instanced Rendering/Attachment Data", order = 50)]
    [Icon("Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Icons/Icon_Attachment.png")]
    [HelpURL("http://docs.blackrosetools.com/InstancedAnimations/html/class_black_rose_projects_1_1_instanced_animation_system_1_1_instanced_attachment_data.html")]
    public class InstancedAttachmentData : ScriptableObject
    {
        /// <summary>
        /// Mesh that will be rendered. Must have enabled Read/Write
        /// </summary>
        public Mesh mesh;
        /// <summary>
        /// Materials for this attachments. Materials shader must support InstancedRendering
        /// </summary>
        public Material[] materials;
        /// <summary>
        /// Name of bone to which attachment will be connected
        /// </summary>
        public string boneName;
        /// <summary>
        /// Position offset from bone pivot
        /// </summary>
        public Vector3 positionOffset = Vector3.zero;
        /// <summary>
        /// Rotation offset used by editor config to calculate quaternion rotation
        /// </summary>
        internal Vector3 rotationOffset = Vector3.zero;
        /// <summary>
        /// Rotation offset from bone pivot
        /// </summary>
        public Vector3 rotationOffsetReal = Vector3.zero;
        /// <summary>
        /// Scale of attachment
        /// </summary>
        public Vector3 scale = Vector3.one;
        /// <summary>
        /// Custom goup ID allow to separate some of same type attachments to allow different transform parameters
        /// </summary>
        public int groupID;
        /// <summary>
        /// Rendering layer for this attachment
        /// </summary>
        [Utility.Layer] public int layer;
        /// <summary>
        /// Shadow casting mode for this attachment
        /// </summary>
        public ShadowCastingMode shadowMode;
        /// <summary>
        /// Will this attachment receive shadows
        /// </summary>
        public bool receiveShadow;
        /// <summary>
        /// Max LOD of parent Instanced Renderer this attachment will be draw
        /// </summary>
        public int maxRenderLOD;
    }

    internal class RuntimeInstancingSharedAttachment
    {
        internal int boneIndex;
        internal int instancesCount;
        internal int meshBakedRigBoneIndex = -1;
        internal int maxLOD;
        internal Mesh originalMesh;
        internal Vector3[] vertices;
        internal Vector3[] normals;
        internal InstancedAttachmentData attachmentData;
        internal InstancedAnimationManager.VertexCache vertexCacheList;
        internal InstancedAnimationManager.MaterialBlock materialBlockList;
    }
}
#endif