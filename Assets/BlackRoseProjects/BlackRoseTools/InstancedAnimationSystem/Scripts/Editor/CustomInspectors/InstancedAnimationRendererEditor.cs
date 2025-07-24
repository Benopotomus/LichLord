#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [CustomEditor(typeof(InstancedAnimationRenderer)), CanEditMultipleObjects]
    internal class InstancedAnimationRendererEditor : Editor
    {
        private static class GUIContents
        {
            public static readonly GUIContent animationData = new GUIContent("Animation", "Animation data that stored both animationa and renderers structure");
            public static readonly GUIContent animator = new GUIContent("Animator", "Baked animator data generated from Unity Animator");
            public static readonly GUIContent cullingMode = new GUIContent("Culling Mode", "Controls what is updated when object has been culled");
            public static readonly GUIContent defaultAnimation = new GUIContent("Default animation", "Allow to select default animation that will start playing after initialization. This option only work if Animator isn't used.");
            public static readonly GUIContent applyRootMotion = new GUIContent("Apply Root Motion", "Automatically move the object using the root motion from baked animations.");
            public static readonly GUIContent autoplay = new GUIContent("Autoplay", "Play constantly animation");
            public static readonly GUIContent animation = new GUIContent("Animation", "Select animation to display in debug");
            public static readonly GUIContent animationFrame = new GUIContent("Frame", "Frame for currently displayed animation");
            public static readonly GUIContent playbackSpeed = new GUIContent("Playback Speed", "Speed of animation playback autoplay debug");
            public static readonly GUIContent showDebug = new GUIContent("Show debug info", "allow to show and hide additional debuging informations");
        }
        private const string ContextPath = "CONTEXT/InstancedAnimationRenderer/Instanced Animation/";
        readonly static Dictionary<Material, MaterialEditor> materials = new Dictionary<Material, MaterialEditor>();
        [SerializeField] private static bool showDebug = false;
        private GUIStyle centerLabel;
        private GUIStyle richLabel;
        private GUIStyle background;

        private SerializedProperty animationData;
        private SerializedProperty animator;
        private SerializedProperty cullingMode;
        private SerializedProperty defaultAnimation;
        private SerializedProperty applyRootMotion;

        private bool requireWarmup;

        private void OnDisable()
        {
            ClearMaterialEditors();
            EditorPrefs.SetBool("BRP_InstancedRenderer_showDebug", showDebug);
        }

        private void OnEnable()
        {
            requireWarmup = true;
            ClearMaterialEditors();
            showDebug = EditorPrefs.GetBool("BRP_InstancedRenderer_showDebug", true);
        }

        private void ClearMaterialEditors()
        {
            foreach (KeyValuePair<Material, MaterialEditor> mat in materials)
            {
                MaterialEditor _materialEditor = mat.Value;
                if (_materialEditor != null)
                    DestroyImmediate(_materialEditor);
            }
            materials.Clear();
        }

        private void Warmup()
        {
            if (!requireWarmup)
                return;
            requireWarmup = false;

            centerLabel = new GUIStyle(GUI.skin.label);
            centerLabel.richText = true;
            centerLabel.alignment = TextAnchor.MiddleCenter;

            richLabel = new GUIStyle(GUI.skin.label);
            richLabel.richText = true;

            background = new GUIStyle(EditorStyles.helpBox);
            background.stretchWidth = true;
            background.border = new RectOffset(5, 5, 5, 5);

            animationData = serializedObject.FindProperty(nameof(InstancedAnimationRenderer.animationData));
            animator = serializedObject.FindProperty(nameof(InstancedAnimationRenderer.animator));
            cullingMode = serializedObject.FindProperty(nameof(InstancedAnimationRenderer.cullingMode));
            defaultAnimation = serializedObject.FindProperty(nameof(InstancedAnimationRenderer.defaultAnimation));
            applyRootMotion = serializedObject.FindProperty(nameof(InstancedAnimationRenderer.applyRootMotion));
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        internal static void DrawGizmoForMyScript(InstancedAnimationRenderer _target, GizmoType gizmoType)
        {
            if (_target.animationData == null)
                return;
            if (InstancedAnimationEditorManager.settings.drawBoundingGizmoSpheres)
            {
                Transform transform = _target.transform;
                Gizmos.color = Color.yellow;
                Vector3 scale = transform.lossyScale;
                Vector3 pos = _target.animationData.boundingSphereOffset;

                pos.x *= scale.x;
                pos.y *= scale.y;
                pos.z *= scale.z;

                Gizmos.DrawWireSphere(transform.position + transform.rotation * pos, transform.lossyScale.FlatScale() * _target.animationData.boundingSphereRadius);
            }
            if (Application.isPlaying || InstancedAnimationEditorManager.settings.editorRenderMode != InstancedAnimationSystemSettings.EditorRenderMode.onlyGizmosSelected)
                return;

            Gizmos.color = Color.white;

            if (_target.animationData != null && _target.animationData.LOD.Length > 0)
            {
                Transform transform = _target.transform;
                for (int i = 0; i < _target.animationData.LOD[0].instancingMeshData.Length; ++i)
                    Gizmos.DrawMesh(_target.animationData.LOD[0].instancingMeshData[i].mesh, transform.position, transform.rotation, transform.lossyScale);
            }
        }

        public override void OnInspectorGUI()
        {
            Warmup();

            serializedObject.UpdateIfRequiredOrScript();
            bool isPlayMode = Application.isPlaying;
            bool multiEdit = targets.Length > 1;

            GUI.enabled = !isPlayMode;
            EditorGUILayout.PropertyField(animationData, GUIContents.animationData);
            bool animationChanged = serializedObject.hasModifiedProperties;
            GUI.enabled = !multiEdit || !isPlayMode;
            EditorGUILayout.PropertyField(animator, GUIContents.animator);
            bool animatorChanged = serializedObject.hasModifiedProperties;
            EditorGUILayout.PropertyField(applyRootMotion, GUIContents.applyRootMotion);
            EditorGUILayout.PropertyField(cullingMode, GUIContents.cullingMode);

            if (!multiEdit)
            {
                InstancedAnimationRenderer instance = (InstancedAnimationRenderer)target;
                if (animationData.objectReferenceValue != null)
                {
                    GUI.enabled = animator.objectReferenceValue == null;
                    DefaultAnimationIndexPropertyDrawer.currentAnimation = (InstancedAnimationData)animationData.objectReferenceValue;
                    EditorGUILayout.PropertyField(defaultAnimation, GUIContents.defaultAnimation);
                    GUI.enabled = true;
                }
                if (serializedObject.hasModifiedProperties && isPlayMode && instance.rendererInstance != null)
                {
                    instance.rendererInstance.CullingMode = (InstancingCullingMode)cullingMode.intValue;
                    instance.rendererInstance.RootMotion = applyRootMotion.boolValue;
                    instance.defaultAnimation = defaultAnimation.intValue;
                    if (instance.rendererInstance != null && animatorChanged)
                        instance.rendererInstance.SetAnimator((InstancedAnimatorData)animator.objectReferenceValue);
                }
                CheckAnimationAnimatorValidity((InstancedAnimationData)animationData.objectReferenceValue, (InstancedAnimatorData)animator.objectReferenceValue);

                showDebug = EditorGUILayout.Toggle(GUIContents.showDebug, showDebug);
                DrawDebugInspector(instance, isPlayMode, animationChanged);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private string FormatVector(Vector4 vector)
        {
            return $"x: {vector.x:0.00} y: {vector.y:0.00} z: {vector.z:0.00} w: {vector.w:0.00}";
        }

        private void DrawDebugInspector(InstancedAnimationRenderer instance, bool isPlayMode, bool animationChanged)
        {
            if (!showDebug)
                return;
            if (!DrawAnimationsInfoBox((InstancedAnimationData)animationData.objectReferenceValue))
                return;
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical(background);
            EditorGUILayout.LabelField("<b>Debug info</b>", centerLabel);

            PrintLODInfo(instance);

            if (instance.rendererInstance != null && instance.rendererInstance.attachments != null)
            {
                EditorGUILayout.BeginVertical(background);
                EditorGUILayout.LabelField("<b>Runtime instance data</b>", centerLabel);
                InstancedAnimationHelper.StringField("Instance id:", instance.rendererInstance.InstanceJobId.ToString(), 150);
                InstancedAnimationHelper.StringField("Attachments:", instance.rendererInstance.attachments.Count.ToString(), 150);
                InstancedAnimationHelper.StringField("Sync bones:", instance.rendererInstance.boneSyncDatas != null ? instance.rendererInstance.boneSyncDatas.Count.ToString() : "0", 150);
                EditorGUILayout.EndVertical();
            }
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
            if (isPlayMode && instance.rendererInstance != null)
            {
                float[] floats = instance.rendererInstance.customFloatValues;
                Vector4[] vectors = instance.rendererInstance.customVectorValues;
                if (floats.Length > 0 || vectors.Length > 0)
                {
                    EditorGUILayout.BeginVertical(background);
                    EditorGUILayout.LabelField("<b>Custom Shader Values</b>", centerLabel);
                    if (floats != null)
                    {
                        for (int i = 0; i < floats.Length; ++i)
                        {
                            CustomValueFloatHolder cvfh = instance.animationData.customFloats[i];
                            EditorGUILayout.LabelField(new GUIContent($"<b>Group:</b> {cvfh.GroupName} (<i>{cvfh.propertyName}</i>) [<b>LOD: </b>{cvfh.lodID} |<b>Mesh: </b>{ cvfh.meshID} |<b>Submesh: </b>{cvfh.submeshID}] {floats[i]:0.000}", floats[i].ToString()), richLabel);
                        }
                    }
                    if (vectors != null)
                    {
                        for (int i = 0; i < vectors.Length; ++i)
                        {
                            CustomValueVectorHolder cvfh = instance.animationData.customVectors[i];
                            EditorGUILayout.LabelField(new GUIContent($"<b>Group:</b> {cvfh.GroupName} (<i>{cvfh.propertyName}</i>) [<b>LOD: </b>{cvfh.lodID} |<b>Mesh: </b>{ cvfh.meshID} |<b>Submesh: </b>{cvfh.submeshID}] {FormatVector(vectors[i])}", vectors[i].ToString()), richLabel);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
            }
#endif

            if (instance.rendererInstance != null && instance.animationData != null)
            {
                if (!isPlayMode)
                {
                    EditorGUILayout.LabelField("<b>Animation rendering debug</b>", centerLabel);
                    if (InstancedAnimationEditorManager.settings.editorRenderMode == InstancedAnimationSystemSettings.EditorRenderMode.renderFull || InstancedAnimationEditorManager.settings.editorRenderMode == InstancedAnimationSystemSettings.EditorRenderMode.onlySelected)
                    {
                        EditorGUILayout.BeginVertical(background);
                        AnimationInfo[] anim = instance.animationData.animations;
                        int totalTime = 0;
                        int[] fps = new int[anim.Length];
                        int[] timeStart = new int[anim.Length];
                        string[] names = new string[anim.Length];
                        for (int i = 0; i < anim.Length; ++i)
                        {
                            timeStart[i] = totalTime;
                            totalTime += anim[i].totalFrame;
                            fps[i] = anim[i].fps;
                            names[i] = anim[i].animationName;
                        }
                        if (instance.editor_selectedAnimation > anim.Length || instance.editor_selectedAnimation == -1)
                            instance.editor_selectedAnimation = 0;

                        instance.editor_selectedAnimation = EditorGUILayout.Popup(GUIContents.animation, instance.editor_selectedAnimation, names);
                        instance.editor_autoPlayMode = EditorGUILayout.Toggle(GUIContents.autoplay, instance.editor_autoPlayMode);
                        if (instance.editor_autoPlayMode)
                        {
                            instance.editor_playbackSpeed = EditorGUILayout.FloatField(GUIContents.playbackSpeed, instance.editor_playbackSpeed);
                        }
                        GUI.enabled = !instance.editor_autoPlayMode;
                        float value = EditorGUILayout.Slider(GUIContents.animationFrame, instance.editor_selectedAnimationFrame, 0, anim[instance.editor_selectedAnimation].totalFrame - 1);
                        GUI.enabled = true;
                        if (!instance.editor_autoPlayMode)
                            instance.editor_selectedAnimationFrame = value;

                        if (animationChanged)
                            materials.Clear();

                        for (int i = 0; i < instance.animationData.LOD.Length; ++i)
                            for (int j = 0; j < instance.animationData.LOD[i].instancingMeshData.Length; ++j)
                                for (int k = 0; k < instance.animationData.LOD[i].instancingMeshData[j].fixedMaterials.Length; ++k)
                                {
                                    Material m = instance.animationData.LOD[i].instancingMeshData[j].fixedMaterials[k];
                                    if (!materials.ContainsKey(m))
                                        materials.Add(m, (MaterialEditor)CreateEditor(m));
                                }
                        EditorGUILayout.EndVertical();
                    }
                }
                else if (instance.rendererInstance._animator != null)
                {
                    EditorGUILayout.BeginVertical(background);
                    EditorGUILayout.LabelField("<b>Animator debug</b>", centerLabel);
                    instance.rendererInstance._animator.PrintInspector();
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
            foreach (KeyValuePair<Material, MaterialEditor> mat in materials)
            {
                MaterialEditor _materialEditor = mat.Value;
                if (_materialEditor != null)
                {
                    _materialEditor.DrawHeader();
                    _materialEditor.OnInspectorGUI();
                }
            }
        }

        private bool DrawAnimationsInfoBox(InstancedAnimationData animations)
        {
            if (animations == null)
                return false;
            else
            {
                int clips = animations.animations.Length;
                int frames = 0;
                bool rootMotion = false;
                int lods = animations.LOD.Length;
                int meshes = 0;
                int customShader = animations.customFloats.Count + animations.customVectors.Count;
#if !BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
                customShader = 0;
#endif
                for (int i = 0; i < clips; ++i)
                {
                    frames += animations.animations[i].totalFrame;
                    if (animations.animations[i].rootMotion)
                        rootMotion = true;
                }
                for (int i = 0; i < lods; ++i)
                    meshes += animations.LOD[i].instancingMeshData.Length;
                EditorGUILayout.HelpBox($"Animation Clips: {clips}\nTotal frames: {frames}\nRoot motion support: {rootMotion}\nTotal Meshes: {meshes}\nLOD levels: {lods}{(customShader > 0 ? "\nCustom shader values in use: " + customShader : "")}", MessageType.Info);
            }
            return true;
        }

        private void CheckAnimationAnimatorValidity(InstancedAnimationData animation, InstancedAnimatorData animator)
        {
            if (animation == null)
            {
                EditorGUILayout.HelpBox("Not selected Baked Animation Data", MessageType.Error);
                return;
            }
            if (animator == null)
                return;
            string missings = "";
            for (int i = 0; i < animator.states.Length; ++i)
            {
                string animName = animator.states[i].animationName;
                bool found = false;
                if (!string.IsNullOrEmpty(animName))
                {
                    for (int j = 0; j < animation.animations.Length; ++j)
                    {
                        if (animName == animation.animations[j].animationName)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    if (missings.Length == 0)
                        missings += "\"" + animName + "\"";
                    else
                        missings += ", \"" + animName + "\"";
                }
            }
            if (missings.Length > 0)
                EditorGUILayout.HelpBox("Used baked animator require animations: " + missings + " that are missing in baked animation data!", MessageType.Error);
        }

        private void PrintLODInfo(InstancedAnimationRenderer instance)
        {
            if (instance.animationData != null && instance.rendererInstance != null)
            {
                EditorGUILayout.BeginVertical(background);
                Camera cam = null;
                if (!Application.isPlaying)
                {
                    var s = SceneView.sceneViews;
                    foreach (var element in s)
                    {
                        Camera c = ((SceneView)element).camera;
                        if (c != null)
                        {
                            cam = c;
                            break;
                        }
                    }
                }
                else
                    cam = Camera.main;
                if (cam is null)
                    return;
                Rect rect = GUILayoutUtility.GetRect(5000, 30);
                InstancedAnimationEditorManager.DrawLODBar(rect, instance.animationData, instance.transform.position, instance.transform.lossyScale.FlatScale(), cam);
                EditorGUILayout.EndVertical();
                Repaint();
            }
        }
        [MenuItem(ContextPath + "Add Attachment", false, 0)]
        static void Context_AddAttachment(MenuCommand command)
        {
            InstancedAnimationRenderer mat = (InstancedAnimationRenderer)command.context;
            Undo.RegisterCreatedObjectUndo(mat.gameObject.AddComponent<InstancedAnimationAttachmentBehaviour>(), ContextPath + "Add Attachment");
        }

        [MenuItem(ContextPath + "Add Bone Sync", false, 1)]
        static void Context_AddBoneSync(MenuCommand command)
        {
            InstancedAnimationRenderer mat = (InstancedAnimationRenderer)command.context;
            Undo.RegisterCreatedObjectUndo(mat.gameObject.AddComponent<InstancedAnimationBoneSyncBehaviour>(), ContextPath + "Add Bone Sync");
        }
    }
}
#endif