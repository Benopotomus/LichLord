#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BlackRoseProjects.Utility;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class InstancedRendererConfigurator : EditorWindow
    {
        private static class GUIContents
        {
            public static readonly GUIContent animationData = new GUIContent("Animation data", "Generated animated data");
            public static readonly GUIContent resetCamera = new GUIContent("Reset Camera", "Reset camera position to start position");
            public static readonly GUIContent drawGrid = new GUIContent("Draw grid", "Draw grid floor");
            public static readonly GUIContent foldout_showBounding = new GUIContent("Bounding Sphere Config", "Display bounding sphere configurator");
            public static readonly GUIContent foldout_attachmentConfig = new GUIContent("Attachments Config", "Display attachments configurator");
            public static readonly GUIContent foldout_lodConfig = new GUIContent("LOD Config", "Display LOD configurator");
            public static readonly GUIContent neverCull = new GUIContent("Never Cull", "Don't cull this LOD at distance. This will render given instance");
            public static readonly GUIContent bone = new GUIContent("Bone", "Bone to attach attachment");
            public static readonly GUIContent mesh = new GUIContent("Mesh", "Attachment mesh. Must have enabled Read/Write");
            public static readonly GUIContent materialsLabel = new GUIContent("Materials", "Materials for this mesh to render");
            public static readonly GUIContent positionOffset = new GUIContent("Position offset", "Offset for position of given attachment. Position is local for bone");
            public static readonly GUIContent rotationOffset = new GUIContent("Rotation offset", "Offset for rotation of given attachment. Rotation is local for bone");
            public static readonly GUIContent scale = new GUIContent("Scale", "Scale for target attachment. Scale is absolute");
            public static readonly GUIContent additionalName = new GUIContent("Additional Data", "Additional data for attachments to be used during rendering");
            public static readonly GUIContent groupID = new GUIContent("Group ID", "ID of group to be used by given attachment. Setting different groups allow to separate batches even for same meshes and materials, allowing to use separate position rotation and scale config");
            public static readonly GUIContent layer = new GUIContent("Layer", "Layer on which attachment will be rendered");
            public static readonly GUIContent attachmentLOD = new GUIContent("Max parent LOD", "Max level of parent Instanced Animation Renderer LOD this attachment will be rendered. Attachment is always culled when parent renderer is culled.");
            public static readonly GUIContent receiveShadows = new GUIContent("Receive Shadows", "Will attachment receive shadows");
            public static readonly GUIContent castShadows = new GUIContent("Cast Shadows", "Will attachment cast shadows");
            public static readonly GUIContent coppyAttachmentToClipboard = new GUIContent("Coppy attachment data to clipboard", "Coppy data from attachment to system clipboard. Copied data can be pasted into InstancedAttachmentData");
            public static readonly GUIContent pasteAttachmentFromClipboard = new GUIContent("Paset attachment data from clipboard", "Paste data to attachment from system clipboard. Data can be copied from any InstancedAttachmentData");
            public static readonly GUIContent save = new GUIContent("Save", "Save given config to file");
            public static readonly GUIContent clearAttachments = new GUIContent("Clear", "Clear attachments config");
            public static readonly GUIContent sphereDebug = new GUIContent("Sphere Color", "Color of bounding sphere. Used only for debug");
            public static readonly GUIContent boundingOffset = new GUIContent("Sphere pivot", "Pivot of bounding sphere for culling. Set this value to encapsulate whole mesh by sphere");
            public static readonly GUIContent boundingSize = new GUIContent("Sphere radius", "Radius of bounding sphere. Adjust this value to encapsulate whole mesh");
            public static readonly GUIContent animation = new GUIContent("Animation", "Select animation to show in preview");
            public static readonly GUIContent transitionFromAnim = new GUIContent("Start animation", "Select animation to play before transition");
            public static readonly GUIContent transitionToAnim = new GUIContent("End animation", "Select animation to switch during playtime by transition");
            public static readonly GUIContent transitionDuration = new GUIContent("Transition duration", "Duration (in seconds) of transition between these animations");
            public static readonly GUIContent transitionReset = new GUIContent("Reset animation", "Reset transition and animation");
            public static readonly GUIContent transitionPlay = new GUIContent("Start transition", "Starts transition now");
            public static readonly GUIContent autoPlay = new GUIContent("Auto play", "Automatically play animation in loop");
            public static readonly GUIContent animationFrame = new GUIContent("Animation frame", "Allow to set specified frame of animation. Decimal value are obtained by interpolation");
            public static readonly GUIContent animationPlaybackSpeed = new GUIContent("Playback speed", "Speed for auto playing animation");
            public static readonly GUIContent foldout_transition = new GUIContent("Transition preview", "Show debug for transitions");
            public static readonly GUIContent transitionFrom = new GUIContent("Previous animation", "Select animation from which transition will be performed");
            public static readonly GUIContent transitionProgress = new GUIContent("Transition", "Select step in transition between animations");
            public static readonly GUIContent createNewMaterialButton = new GUIContent("Create new", "Create new material with supported Instanced Animation System");
            public static readonly GUIContent changeShaderButton = new GUIContent("Change Shader", "Change shader of selected material for one that is supported with Instanced Animation System");
            public static readonly GUIContent changeShaderOnCopyButton = new GUIContent("Change on copy", "Create copy of used material and change shader for  one that is supported with Instanced Animation System");
            public static readonly GUIContent helpButton = new GUIContent(EditorGUIUtility.IconContent("_Help@2x"));

            static GUIContents()
            {
                helpButton.tooltip = "Open online documentation";
            }

        }

        internal enum OpenOption { none, bounding, attachment, lod };
        internal static InstancedRendererConfigurator managerWindow;

        private readonly string[] gizmoShaders = new string[4] { "BlackRoseProjects/InstancedAnimationSystem/Built-in/Legacy Shaders/Diffuse", "BlackRoseProjects/InstancedAnimationSystem/Universal Render Pipeline/BRP-Lit", "BlackRoseProjects/InstancedAnimationSystem/HDRP/BRP-Lit", "" };

        private InstancedAnimationData selectedData;

        private List<Camera> cameras = new List<Camera>();

        private InstancedAnimationRenderer irb;
        internal RenderTexture renderTexture;
        private Camera renderCamera;
        private Light[] renderLight;

        private Material gizmoMaterialX;
        private Material gizmoMaterialY;
        private Material gizmoMaterialZ;

        private Material gizmoMaterialX_anim;
        private Material gizmoMaterialY_anim;
        private Material gizmoMaterialZ_anim;

        private Material gridMaterial;

        private Material boundingSphereMaterial;
        private Mesh boundingSphereMesh;
        private Mesh planeMesh;
        private Mesh boneHandleMesh;
        private Mesh gizmo;
        private Texture gridTexture;
        private Texture tooltipTexture;

        private Vector3 centerOfSystem = new Vector3(2048, 2048, 2048);
        private Vector3 centerOfSystemSpawnPoint = new Vector3(2048, 2048, 2048);
        private Rect textureRenderRect = new Rect(0, 25, 512, 512);
        private Rect tooltipRect = new Rect(512 - 32, 25 + 512 - 64, 32, 64);
        private Rect tooltip1Rect = new Rect(512 - 128 - 32, 25 + 512 - 64, 128, 32);
        private Rect tooltip2Rect = new Rect(512 - 128 - 32, 25 + 512 - 32, 128, 32);

        private bool showBounding;
        private bool drawGrid = true;
        private bool showAttachements;
        private bool advancedAttachmentData;
        private bool showLod = false;

        private Color boundingColor;
        private int selectedBone;
        bool updateMeshees = false;
        bool allowMouseDrag = false;
        [SerializeField] bool animationMode = true;
        Vector2 scrollRect;

        [SerializeField] private Mesh originalAttachement;
        private Mesh instancedAttachement;
        [SerializeField] private Material[] materials = new Material[0];
        [SerializeField] private Shader[] shaderCache = new Shader[0];

        [SerializeField] private Vector3 attachmentPos;
        [SerializeField] private Vector3 attachmenRot;
        [SerializeField] private Vector3 attachmenRotTrue;
        [SerializeField] private Quaternion attachmenRotFinal = Quaternion.identity;
        [SerializeField] private Vector3 attachmenScale = Vector3.one;
        [SerializeField] private int attachmentGroupID;
        [SerializeField] private int attachmentlayerID;
        [SerializeField] private bool attachmentReceiveShadows = true;
        [SerializeField] private int attachmentLOD = 4;
        [SerializeField] private ShadowCastingMode attachmentShadows = ShadowCastingMode.On;

        [SerializeField] private Vector3 attachmentPosReal;
        [SerializeField] private Vector3 attachmenRotReal;

        bool enabled = false;
        public static bool isVisable;

        [MenuItem("Tools/Black Rose Projects/Instanced Animation System/Configurator Helper", false, 10)]
        static void MakeWindow()
        {
            managerWindow = GetWindow<InstancedRendererConfigurator>("Configurator Helper");
            managerWindow.minSize = new Vector2(960, 512 + 20);//960
        }

        internal static void OpenAttachmentConfig(InstancedAttachmentData attachmentData)
        {
            if (managerWindow == null)
                managerWindow = GetWindow<InstancedRendererConfigurator>("Configurator Helper");
            if (isVisable && managerWindow.selectedData != null)
            {
                managerWindow.showBounding = false;
                managerWindow.showAttachements = true;
                managerWindow.showLod = false;
                managerWindow.LoadAttachmentFromSettings(attachmentData);
            }
        }

        internal static void OpenForConfig(InstancedAnimationData toOpen, OpenOption openOption = OpenOption.none)
        {
            MakeWindow();
            if (toOpen != null)
                managerWindow.selectedData = toOpen;
            switch (openOption)
            {
                case OpenOption.none:
                    managerWindow.showBounding = false;
                    managerWindow.showAttachements = false;
                    managerWindow.showLod = false;
                    break;
                case OpenOption.lod:
                    managerWindow.showBounding = false;
                    managerWindow.showAttachements = false;
                    managerWindow.showLod = true;
                    break;
                case OpenOption.bounding:
                    managerWindow.showBounding = true;
                    managerWindow.showAttachements = false;
                    managerWindow.showLod = false;
                    break;
                case OpenOption.attachment:
                    managerWindow.showBounding = false;
                    managerWindow.showAttachements = true;
                    managerWindow.showLod = false;
                    break;
            }
            if (managerWindow.irb != null)
                DestroyImmediate(managerWindow.irb.gameObject);
            managerWindow.ConfigNewInstance();
        }

        internal static bool IsOpenAndReady()
        {
            return isVisable && managerWindow != null && managerWindow.irb != null;
        }

        protected void OnLostFocus()
        {
            isVisable = false;
        }

        private void OnDisable()
        {
            enabled = false;
            if (irb != null)
                DestroyImmediate(irb.gameObject);
            EditorApplication.update -= MyRendering;
            AssemblyReloadEvents.afterAssemblyReload -= AfterAssemblyReload;
            Undo.undoRedoPerformed -= UndoRedo;
            renderCamera.targetTexture = null;
            DestroyImmediate(renderCamera.gameObject);
            DestroyImmediate(renderTexture);
            DestroyImmediate(boundingSphereMaterial);
            DestroyImmediate(gizmoMaterialX);
            DestroyImmediate(gizmoMaterialY);
            DestroyImmediate(gizmoMaterialZ);
            DestroyImmediate(gizmoMaterialX_anim);
            DestroyImmediate(gizmoMaterialY_anim);
            DestroyImmediate(gizmoMaterialZ_anim);
            DestroyImmediate(gridMaterial);

            DestroyImmediate(boneHandleMesh);
            DestroyImmediate(instancedAttachement);
            isVisable = false;
        }

        private void OnEnable()
        {
            enabled = true;
            EditorApplication.update += MyRendering;
            AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReload;
            Undo.undoRedoPerformed += UndoRedo;

            renderTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32, 0);
            renderCamera = new GameObject().AddComponent<Camera>();
            renderCamera.clearFlags = CameraClearFlags.Color;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            renderCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            renderCamera.enabled = false;
            renderCamera.renderingPath = RenderingPath.DeferredShading;
            renderCamera.targetTexture = renderTexture;

            ResetCamera();

            gizmo = AssetDatabase.LoadAssetAtPath<Mesh>(InstancedRenderingResources.PivotModel);
            gridTexture = AssetDatabase.LoadAssetAtPath<Texture>(InstancedRenderingResources.GridTexture);
            tooltipTexture = AssetDatabase.LoadAssetAtPath<Texture>(InstancedRenderingResources.TooltipTexture);

            Light l = renderCamera.gameObject.AddComponent<Light>();
            l.type = LightType.Directional;
            l.range = 50;
            l.intensity = 1;
            l.spotAngle = 160;
            l.enabled = false;
            renderLight = new Light[] { l };
            attachmenRotFinal = Quaternion.identity;

            Pipelines pipeline = BRPPipelineHelper.GetCurrentPipeline();

            gizmoMaterialX = new Material(Shader.Find("Unlit/Color"));
            gizmoMaterialX.color = Color.red;
            gizmoMaterialY = new Material(Shader.Find("Unlit/Color"));
            gizmoMaterialY.color = Color.green;
            gizmoMaterialZ = new Material(Shader.Find("Unlit/Color"));
            gizmoMaterialZ.color = Color.blue;

            gizmoMaterialX_anim = new Material(Shader.Find(gizmoShaders[(int)pipeline]));
            gizmoMaterialX_anim.color = Color.red;
            gizmoMaterialY_anim = new Material(Shader.Find(gizmoShaders[(int)pipeline]));
            gizmoMaterialY_anim.color = Color.green;
            gizmoMaterialZ_anim = new Material(Shader.Find(gizmoShaders[(int)pipeline]));
            gizmoMaterialZ_anim.color = Color.blue;

            boundingSphereMaterial = new Material(Shader.Find("Hidden/BlackRoseProjects/Build-in/RimEffect"));
            boundingColor = new Color(0.7924528f, 0.6364582f, 0f, 1f);
            boundingSphereMaterial.color = boundingColor;

            gridMaterial = new Material(Shader.Find("Unlit/Texture"));
            gridMaterial.mainTexture = gridTexture;

            GameObject gm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boundingSphereMesh = gm.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(gm);

            gm = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeMesh = gm.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(gm);

            boneHandleMesh = Instantiate(gizmo);
            updateMeshees = true;
        }

        private void UndoRedo()
        {
            updateMeshees = true;
        }

        private void AfterAssemblyReload()
        {
            InstancedAnimationData selectedData = this.selectedData;
            OnDisable();
            OnEnable();
            this.selectedData = selectedData;
            if (selectedData != null)
                ConfigNewInstance();
        }
        private void MyRendering()
        {
            if (selectedData == null || !enabled || !isVisable || !UnityEditorInternal.InternalEditorUtility.isApplicationActive)
                return;
            if (renderCamera == null || renderCamera.targetTexture == null)
            {
                OnDisable();
                OnEnable();
                ConfigNewInstance();
                return;
            }
            Camera lodCamera = renderCamera;
            cameras.Clear();
            cameras.Add(renderCamera);
            irb.EditorOnly_Update(cameras, lodCamera, InstancedAnimationEditorManager.EditorDeltaTime);
            irb.InstancedRenderer.EditorOnly_RefreshMaterials();

            if (drawGrid)
                Graphics.DrawMesh(planeMesh, Matrix4x4.TRS(new Vector3(2048, 2048, 2048), Quaternion.identity, Vector3.one), gridMaterial, 0, renderCamera, 0);

            if (showBounding)
            {
                Vector3 scale = irb.transform.lossyScale;
                Vector3 pos = selectedData.boundingSphereOffset;

                pos.x *= scale.x;
                pos.y *= scale.y;
                pos.z *= scale.z;

                float radius = irb.transform.lossyScale.FlatScale() * selectedData.boundingSphereRadius * 2;

                Matrix4x4 boundingMatrix = Matrix4x4.TRS(irb.transform.position + irb.transform.rotation * pos, Quaternion.identity, new Vector3(radius, radius, radius));
                Graphics.DrawMesh(boundingSphereMesh, boundingMatrix, boundingSphereMaterial, 1, renderCamera, 0);
                DrawGizmo(irb.transform.position + irb.transform.rotation * pos, Quaternion.identity, 1.25f);
            }
            else if (showAttachements)
            {
                int lod = irb.EditorOnly_CalcLodLevels(lodCamera);
                if (lod != -1 && attachmentLOD >= lod)
                {
                    irb.InstancedRenderer.EditorOnly_RenderMesh(renderCamera, boneHandleMesh, gizmoMaterialY_anim, 0);
                    irb.InstancedRenderer.EditorOnly_RenderMesh(renderCamera, boneHandleMesh, gizmoMaterialZ_anim, 1);
                    irb.InstancedRenderer.EditorOnly_RenderMesh(renderCamera, boneHandleMesh, gizmoMaterialX_anim, 2);

                    if (instancedAttachement != null)
                    {
                        int max = math.min(instancedAttachement.subMeshCount, materials.Length);
                        for (int i = 0; i < max; ++i)
                            if (materials[i] != null)
                                irb.InstancedRenderer.EditorOnly_RenderMesh(renderCamera, instancedAttachement, materials[i], i);

                    }
                }
            }
            else
                DrawGizmo(irb.transform.position, Quaternion.identity, 1.25f);

            UnityEditorInternal.InternalEditorUtility.SetCustomLighting(renderLight, Color.white);

            renderCamera.RenderDontRestore();
            Repaint();
            UnityEditorInternal.InternalEditorUtility.RemoveCustomLighting();
        }

        private void DrawGizmo(Vector3 pos, Quaternion quat, float size)
        {
            Matrix4x4 mat = Matrix4x4.TRS(pos, quat, new Vector3(size, size, size));
            Graphics.DrawMesh(gizmo, mat, gizmoMaterialX, 0, renderCamera, 0);
            Graphics.DrawMesh(gizmo, mat, gizmoMaterialY, 0, renderCamera, 1);
            Graphics.DrawMesh(gizmo, mat, gizmoMaterialZ, 0, renderCamera, 2);
        }

        private void ResetCamera()
        {
            centerOfSystem = centerOfSystemSpawnPoint + new Vector3(0, 1.25f, 0);
            renderCamera.transform.position = centerOfSystem + new Vector3(0, 0, 5);
            renderCamera.transform.rotation = Quaternion.Euler(0, 180, 0);
            renderCamera.transform.RotateAround(centerOfSystem, Vector3.up, 20);
        }

        private void OnGUI()
        {
            isVisable = true;
            GUILayout.BeginHorizontal();

            InstancedAnimationData selected = (InstancedAnimationData)EditorGUILayout.ObjectField(GUIContents.animationData, selectedData, typeof(InstancedAnimationData), false);
            if (GUILayout.Button(GUIContents.helpButton, EditorStyles.toolbarButton, GUILayout.MaxWidth(30)))
                Application.OpenURL("http://docs.blackrosetools.com/InstancedAnimations/html/windowconfig.html");
            GUILayout.EndHorizontal();

            if (selected != selectedData)
            {
                Undo.RecordObject(this, "Change aniData");
                selectedData = selected;
                if (irb != null)
                    DestroyImmediate(irb.gameObject);
                if (selected != null)
                    ConfigNewInstance();
            }

            if (selected == null || irb.rendererInstance == null)
                return;

            GUIStyle tooltips = new GUIStyle(GUI.skin.label);
            tooltips.fontSize = 14;
            tooltips.alignment = TextAnchor.MiddleRight;
            tooltips.fontStyle = FontStyle.Bold;
            tooltips.normal.textColor = Color.black;

            GUI.DrawTexture(textureRenderRect, renderTexture);
            GUI.DrawTexture(tooltipRect, tooltipTexture);
            GUI.enabled = false;
            GUI.TextField(tooltip1Rect, "height", tooltips);
            GUI.TextField(tooltip2Rect, "rotation", tooltips);
            GUI.enabled = true;
            //end of render image
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(512);

            EditorGUILayout.BeginVertical();
            scrollRect = EditorGUILayout.BeginScrollView(scrollRect, new GUIStyle(), GUI.skin.verticalScrollbar);
            var CameraLodRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - textureRenderRect.x - textureRenderRect.width - 10, 1);

            if (GUILayout.Button(GUIContents.resetCamera))
                ResetCamera();

            drawGrid = EditorGUILayout.Toggle(GUIContents.drawGrid, drawGrid);
            EditorGUILayout.Space(5);
            DrawAnimations();
            EditorGUILayout.Space(5);

            showBounding = EditorGUILayout.BeginFoldoutHeaderGroup(showBounding, GUIContents.foldout_showBounding);
            if (showBounding) DrawBounding();
            EditorGUILayout.EndFoldoutHeaderGroup();

            showAttachements = EditorGUILayout.BeginFoldoutHeaderGroup(showAttachements, GUIContents.foldout_attachmentConfig);
            if (showAttachements) DrawAttachments();
            EditorGUILayout.EndFoldoutHeaderGroup();

            showLod = EditorGUILayout.BeginFoldoutHeaderGroup(showLod, GUIContents.foldout_lodConfig);
            if (showLod) DrawLod();
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUIEvents();
        }

        private void DrawLod()
        {
            showBounding = false;
            showAttachements = false;
            EditorGUILayout.Space(5);
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - textureRenderRect.x - textureRenderRect.width, 30);
            rect.x += 5;
            rect.width -= 5;
            InstancedAnimationEditorManager.DrawLODBar(rect, selectedData, centerOfSystem, 1f, renderCamera);

            EditorGUI.indentLevel++;


            for (int i = 0; i < selectedData.LOD.Length; ++i)
            {
                InstancingLODData current = selectedData.LOD[i];
                InstancingLODData previous = null;
                InstancingLODData next = null;
                if (i > 0)
                    previous = selectedData.LOD[i - 1];
                if (i < selectedData.LOD.Length - 1)
                    next = selectedData.LOD[i + 1];

                if (next != null)
                {

                }
                else
                {
                    if (current.height == -1)
                    {
                        if (!EditorGUILayout.Toggle(GUIContents.neverCull, true))
                        {
                            Undo.RecordObject(selectedData, "LOD change");
                            current.height = previous != null ? previous.height + 1 : 150;
                            UpdateLODFloat(current, i, selectedData);
                        }
                    }
                    else
                    {
                        if (EditorGUILayout.Toggle(GUIContents.neverCull, false))
                        {
                            Undo.RecordObject(selectedData, "LOD change");
                            current.height = -1;
                            UpdateLODFloat(current, i, selectedData);
                        }
                        //float v1 = EditorGUILayout.FloatField("LOD" + i, current.height);
                        //if (v1 != current.height)
                        //{
                        //    Undo.RecordObject(selectedData, "LOD change");
                        //    current.height = v1;
                        //    UpdateLODFloat(current, i, selectedData);
                        //}
                    }
                }
            }

            for (int i = 0; i < selectedData.LOD.Length; ++i)
            {
                InstancingLODData current = selectedData.LOD[i];
                InstancingLODData previous = null;
                InstancingLODData next = null;
                if (i > 0)
                    previous = selectedData.LOD[i - 1];
                if (i < selectedData.LOD.Length - 1)
                    next = selectedData.LOD[i + 1];

                if (next != null)
                {
                    float nextMax = next.height == -1 ? 1000 * (i + 1) : next.height;

                    float h;
                    if (next.height == -1)
                    {
                        h = Mathf.Max(0f, EditorGUILayout.FloatField("LOD" + i + " -> LOD " + (i + 1), current.height));
                    }
                    else
                        h = Mathf.Max(0f, EditorGUILayout.Slider("LOD" + i + " -> LOD " + (i + 1), current.height, previous != null ? previous.height : 0, nextMax));
                    if (h != current.height)
                    {
                        Undo.RecordObject(selectedData, "LOD change");
                        current.height = h;
                        UpdateLODFloat(current, i, selectedData);
                    }
                }
                else
                {
                    if (current.height == -1)
                    {
                        //if (!EditorGUILayout.Toggle(GUIContents.neverCull, true))
                        //{
                        //    Undo.RecordObject(selectedData, "LOD change");
                        //    current.height = previous != null ? previous.height + 1 : 150;
                        //    UpdateLODFloat(current, i, selectedData);
                        //}
                    }
                    else
                    {
                        //if (EditorGUILayout.Toggle(GUIContents.neverCull, false))
                        //{
                        //    Undo.RecordObject(selectedData, "LOD change");
                        //    current.height = -1;
                        //    UpdateLODFloat(current, i, selectedData);
                        //}
                        float v1 = Mathf.Max(0f, EditorGUILayout.FloatField("Cull distance", current.height));
                        if (v1 != current.height)
                        {
                            Undo.RecordObject(selectedData, "LOD change");
                            current.height = v1;
                            UpdateLODFloat(current, i, selectedData);
                        }
                    }
                }
            }
            if (CheckLodOverlap(selectedData))
                EditorGUILayout.HelpBox("2 or more LOD's groups are overlaping! Consider changing LOD groups distances", MessageType.Warning);
            EditorGUI.indentLevel--;
        }

        private bool CheckLodOverlap(InstancedAnimationData data)
        {
            float min = -1;
            for (int i = 0; i < data.LOD.Length; ++i)
            {
                float h = data.LOD[i].height;
                if (h == min && h != -1)
                    return true;
                else
                    min = h;
            }
            return false;
        }

        private void UpdateLODFloat(InstancingLODData ilod, int lod, InstancedAnimationData data)
        {
            float height = ilod.height;
            switch (lod)
            {
                case 0:
                    data.lodFloat.x = height;
                    break;
                case 1:
                    data.lodFloat.y = height;
                    break;
                case 2:
                    data.lodFloat.z = height;
                    break;
                case 3:
                    data.lodFloat.w = height;
                    break;
            }
            EditorUtility.SetDirty(data);
        }

        private void DrawAttachments()
        {
            showBounding = false;
            showLod = false;
            EditorGUI.indentLevel++;
            if (selectedBone > selectedData.bonesNames.Length)
                selectedBone = 0;
            int newBone = EditorGUILayout.Popup(GUIContents.bone, selectedBone, selectedData.bonesNames);
            Mesh mesh = (Mesh)EditorGUILayout.ObjectField(GUIContents.mesh, originalAttachement, typeof(Mesh), false);
            if (mesh != null)
            {
                if (!mesh.isReadable)
                {
                    EditorUtility.DisplayDialog("Instanced Renderer Configurator", "Selected attachment mesh must have enabled read/Write", "close");
                    EditorGUIUtility.PingObject(mesh);
                    Undo.RecordObject(this, "Value change");
                    mesh = null;
                    originalAttachement = null;
                }
                else
                {
                    EditorGUILayout.LabelField(GUIContents.materialsLabel);

                    EditorGUI.indentLevel++;
                    for (int i = 0; i < materials.Length; ++i)
                    {
                        EditorGUILayout.BeginHorizontal();
                        Material m = (Material)EditorGUILayout.ObjectField("Element " + i, materials[i], typeof(Material), false);
                        if (m == null)
                        {
                            if (GUILayout.Button(GUIContents.createNewMaterialButton))
                            {
                                m = new Material(InstancedAnimationHelper.GetInstancedShaderForShader(null));
                                m.name = selectedData.name + "AttachmentMeterial";
                                string path = AssetDatabase.GetAssetPath(selectedData);
                                path = path.Substring(0, path.LastIndexOf('/') + 1) + m.name + ".mat";
                                path = AssetDatabase.GenerateUniqueAssetPath(path);
                                AssetDatabase.CreateAsset(m, path);
                                EditorGUIUtility.PingObject(m);
                            }
                        }
                        if (m != materials[i])
                        {
                            Undo.RecordObject(this, "Value change");
                            materials[i] = m;
                        }
                        EditorGUILayout.EndHorizontal();
                        if (m != null && shaderCache[i] != m.shader)
                        {
                            shaderCache[i] = m.shader;
                            if (!InstancedAnimationHelper.CheckIfIncludeInstanced(m))
                            {
                                shaderCache[i] = null;
                                EditorGUILayout.HelpBox("Used shader is not supporting Instanced Animation Rendering. Use one of supporteds shaders.", MessageType.Error);

                                EditorGUILayout.BeginHorizontal();

                                if (GUILayout.Button(GUIContents.changeShaderButton))
                                {
                                    Undo.RecordObject(m, "Value Shader");
                                    m.shader = InstancedAnimationHelper.GetInstancedShaderForShader(m.shader);
                                    InstancedAnimationHelper.FixMaterialData(m, selectedData.bonePerVertex);
                                    shaderCache[i] = m.shader;
                                }
                                if (GUILayout.Button(GUIContents.changeShaderOnCopyButton))
                                {
                                    Material m2 = new Material(m);
                                    m2.shader = InstancedAnimationHelper.GetInstancedShaderForShader(m.shader);
                                    InstancedAnimationHelper.FixMaterialData(m2, selectedData.bonePerVertex);
                                    m2.name = m.name + "Attachment";
                                    string path = AssetDatabase.GetAssetPath(m);
                                    path = path.Substring(0, path.LastIndexOf('/') + 1) + m2.name + ".mat";
                                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                                    AssetDatabase.CreateAsset(m2, path);

                                    EditorGUIUtility.PingObject(m2);

                                    Undo.RecordObject(this, "Value change");
                                    materials[i] = m2;
                                    shaderCache[i] = m2.shader;
                                }

                                EditorGUILayout.EndHorizontal();
                            }
                        }

                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.BeginChangeCheck();
            Vector3 pos = EditorGUILayout.Vector3Field(GUIContents.positionOffset, attachmentPos);
            Vector3 rot = EditorGUILayout.Vector3Field(GUIContents.rotationOffset, attachmenRot);
            Vector3 scale = EditorGUILayout.Vector3Field(GUIContents.scale, attachmenScale);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Value change");
                attachmentPos = pos;
                ConvertRealToFixed(rot);
                // attachmenRot = rot;
                attachmenScale = scale;
                updateMeshees = true;
            }

            if (mesh != originalAttachement)
            {
                DestroyImmediate(instancedAttachement);
                Undo.RecordObject(this, "Value change");
                originalAttachement = mesh;
                if (mesh != null)
                {
                    instancedAttachement = Instantiate(originalAttachement);
                    Undo.RegisterCreatedObjectUndo(instancedAttachement, "Value change");
                    updateMeshees = true;
                    int submeshes = mesh.subMeshCount;
                    if (materials.Length != submeshes)
                    {
                        materials = new Material[submeshes];
                        shaderCache = new Shader[submeshes];
                    }
                }
                else
                {
                    materials = new Material[0];
                    shaderCache = new Shader[0];
                }
            }

            if (newBone != selectedBone || updateMeshees)
            {
                selectedBone = newBone;
                updateMeshees = false;
                boneHandleMesh.bounds = selectedData.LOD[0].instancingMeshData[0].mesh.bounds;
                InstancedAnimationManager.BindPoseToMesh(boneHandleMesh, gizmo.vertices, gizmo.normals, selectedBone, true, selectedData.bindPoses[selectedBone], Vector3.one, Vector3.zero, attachmenRotFinal);
                if (originalAttachement != null)
                {
                    if (instancedAttachement == null)
                        instancedAttachement = Instantiate(originalAttachement);
                    instancedAttachement.bounds = selectedData.LOD[0].instancingMeshData[0].mesh.bounds;
                    InstancedAnimationManager.BindPoseToMesh(instancedAttachement, originalAttachement.vertices, originalAttachement.normals, selectedBone, true, selectedData.bindPoses[selectedBone], attachmenScale, attachmentPos, attachmenRotFinal);
                }
            }
            advancedAttachmentData = EditorGUILayout.Foldout(advancedAttachmentData, GUIContents.additionalName);
            if (advancedAttachmentData)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                int attachmentGroupID = EditorGUILayout.IntField(GUIContents.groupID, this.attachmentGroupID);
                int lod = EditorGUILayout.IntSlider(GUIContents.attachmentLOD, attachmentLOD, 0, selectedData.LOD.Length - 1);
                int attachmentlayerID = EditorGUILayout.LayerField(GUIContents.layer, this.attachmentlayerID);
                bool attachmentReceiveShadows = EditorGUILayout.Toggle(GUIContents.receiveShadows, this.attachmentReceiveShadows);
                ShadowCastingMode attachmentShadows = (ShadowCastingMode)EditorGUILayout.EnumPopup(GUIContents.castShadows, this.attachmentShadows);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Value change");
                    this.attachmentGroupID = attachmentGroupID;
                    this.attachmentlayerID = attachmentlayerID;
                    this.attachmentReceiveShadows = attachmentReceiveShadows;
                    this.attachmentShadows = attachmentShadows;
                    this.attachmentLOD = lod;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(GUIContents.coppyAttachmentToClipboard))
            {
                InstancedAttachmentData iad = CreateInstance<InstancedAttachmentData>();
                iad.mesh = originalAttachement;
                iad.materials = new Material[materials.Length];
                materials.CopyTo(iad.materials, 0);
                iad.boneName = selectedData.bonesNames[selectedBone];
                iad.groupID = attachmentGroupID;
                iad.layer = attachmentlayerID;
                iad.positionOffset = attachmentPos;
                iad.maxRenderLOD = attachmentLOD;
                iad.rotationOffset = attachmenRot;
                iad.rotationOffsetReal = attachmenRotFinal.eulerAngles;
                iad.scale = attachmenScale;
                iad.receiveShadow = attachmentReceiveShadows;
                iad.shadowMode = attachmentShadows;
                GUIUtility.systemCopyBuffer = EditorJsonUtility.ToJson(iad, false);
                DestroyImmediate(iad);
            }

            if (GUILayout.Button(GUIContents.pasteAttachmentFromClipboard))
            {
                string json = GUIUtility.systemCopyBuffer;
                InstancedAttachmentData iad = CreateInstance<InstancedAttachmentData>();
                bool result;
                try
                {
                    EditorJsonUtility.FromJsonOverwrite(json, iad);
                    result = true;
                }
                catch (System.ArgumentException)
                {
                    result = false;
                }
                if (result)
                    OpenAttachmentConfig(iad);
                DestroyImmediate(iad);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = originalAttachement != null && materials.Length > 0;
            if (GUILayout.Button(GUIContents.save))
            {
                string path = AssetDatabase.GetAssetPath(selectedData);
                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Substring(0, path.LastIndexOf("/"));
                    path = OpenSaveFileWindow(path, "Attachment");
                    if (!string.IsNullOrEmpty(path))
                    {
                        string directoryPath = path.Substring(0, path.LastIndexOf("/") + 1);
                        string assetName = path.Substring(path.LastIndexOf("/") + 1);
                        assetName = assetName.Substring(0, assetName.LastIndexOf("."));
                        InstancedAttachmentData iad = CreateInstance<InstancedAttachmentData>();
                        iad.mesh = originalAttachement;
                        iad.materials = new Material[materials.Length];
                        materials.CopyTo(iad.materials, 0);
                        // iad.materials = materials.ToArray();
                        iad.boneName = selectedData.bonesNames[selectedBone];
                        iad.groupID = attachmentGroupID;
                        iad.layer = attachmentlayerID;
                        iad.positionOffset = attachmentPos;
                        iad.rotationOffset = attachmenRot;
                        iad.rotationOffsetReal = attachmenRotFinal.eulerAngles;
                        iad.scale = attachmenScale;
                        iad.maxRenderLOD = attachmentLOD;
                        iad.receiveShadow = attachmentReceiveShadows;
                        iad.shadowMode = attachmentShadows;
                        iad.name = assetName;

                        BRPAssetsHelper.CreateAsset(iad, path);
                        EditorGUIUtility.PingObject(iad);
                    }
                }
            }
            GUI.enabled = true;
            if (GUILayout.Button(GUIContents.clearAttachments))
            {
                DestroyImmediate(instancedAttachement);
                Undo.RecordObject(this, "Value change");
                originalAttachement = null;
                materials = new Material[0];
                shaderCache = new Shader[0];
                this.attachmentGroupID = 0;
                this.attachmentlayerID = selectedData.LOD[0].layer[0];
                this.attachmentReceiveShadows = true;
                this.attachmentShadows = ShadowCastingMode.On;
                this.attachmentLOD = selectedData.LOD.Length - 1;
                attachmentPos = Vector3.zero;
                attachmenRot = Vector3.zero;
                attachmenRotTrue = Vector3.zero;
                // attachmenRotFinal = Quaternion.Euler(attachmenRotTrue);
                attachmenRotFinal = Quaternion.identity;
                attachmenScale = Vector3.one;
                updateMeshees = true;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private void ConvertRealToFixed(Vector3 rotation)
        {
            if (rotation.x != attachmenRot.x)
                attachmenRotTrue = FixRotation(0, rotation, attachmenRot);
            else if (rotation.y != attachmenRot.y)
                attachmenRotTrue = FixRotation(1, rotation, attachmenRot);
            else if (rotation.z != attachmenRot.z)
                attachmenRotTrue = FixRotation(2, rotation, attachmenRot);
            attachmenRot = rotation;
        }

        private Vector3 FixRotation(byte index, Vector3 newRot, Vector3 original)
        {
            float value;
            float old;
            switch (index)
            {
                case 0:
                    {
                        value = newRot.x;
                        old = original.x;
                        attachmenRotFinal = Quaternion.AngleAxis(value - old, attachmenRotFinal * Vector3.right) * attachmenRotFinal;
                        return attachmenRotFinal.eulerAngles;
                    }
                case 1:
                    {
                        value = newRot.y;
                        old = original.y;
                        attachmenRotFinal = Quaternion.AngleAxis(value - old, attachmenRotFinal * Vector3.up) * attachmenRotFinal;
                        return attachmenRotFinal.eulerAngles;
                    }
                case 2:
                    {
                        value = newRot.z;
                        old = original.z;
                        attachmenRotFinal = Quaternion.AngleAxis(value - old, attachmenRotFinal * Vector3.forward) * attachmenRotFinal;
                        return attachmenRotFinal.eulerAngles;
                    }
            }
            return original;
        }

        private void LoadAttachmentFromSettings(InstancedAttachmentData data)
        {
            int boneID = -1;
            for (int i = 0; i < selectedData.bonesNames.Length; ++i)
                if (selectedData.bonesNames[i] == data.boneName)
                {
                    boneID = i;
                    break;
                }
            if (boneID == -1)
                return;
            materials = new Material[data.materials.Length];
            shaderCache = new Shader[data.materials.Length];
            data.materials.CopyTo(materials, 0);
            attachmentPos = data.positionOffset;
            attachmenRot = data.rotationOffset;
            attachmenRotTrue = data.rotationOffsetReal;
            attachmenRotFinal = Quaternion.Euler(attachmenRotTrue);
            attachmenScale = data.scale;
            updateMeshees = true;

            if (data.mesh != originalAttachement)
            {
                DestroyImmediate(instancedAttachement);
                originalAttachement = data.mesh;
                if (data.mesh != null)
                {
                    instancedAttachement = Instantiate(originalAttachement);
                    updateMeshees = true;
                }
            }

            if (boneID != selectedBone || updateMeshees)
            {
                selectedBone = boneID;
                updateMeshees = false;
                boneHandleMesh.bounds = selectedData.LOD[0].instancingMeshData[0].mesh.bounds;
                InstancedAnimationManager.BindPoseToMesh(boneHandleMesh, gizmo.vertices, gizmo.normals, selectedBone, true, selectedData.bindPoses[selectedBone], Vector3.one, Vector3.zero, attachmenRotFinal);
                if (originalAttachement != null)
                {
                    if (instancedAttachement == null)
                        instancedAttachement = Instantiate(originalAttachement);
                    instancedAttachement.bounds = selectedData.LOD[0].instancingMeshData[0].mesh.bounds;
                    InstancedAnimationManager.BindPoseToMesh(instancedAttachement, originalAttachement.vertices, originalAttachement.normals, selectedBone, true, selectedData.bindPoses[selectedBone], attachmenScale, attachmentPos, attachmenRotFinal);
                }
            }
            attachmentGroupID = data.groupID;
            attachmentlayerID = data.layer;
            attachmentReceiveShadows = data.receiveShadow;
            attachmentShadows = data.shadowMode;
            attachmentLOD = data.maxRenderLOD;
        }

        private void DrawBounding()
        {
            showAttachements = false;
            showLod = false;
            EditorGUI.indentLevel++;
            Color c = EditorGUILayout.ColorField(GUIContents.sphereDebug, boundingColor);
            if (c != boundingColor)
            {
                Undo.RecordObject(this, "Change boundingColor");
                boundingColor = c;
                boundingSphereMaterial.color = c;
            }
            EditorGUI.BeginChangeCheck();
            float3 boundingSphereOffset = EditorGUILayout.Vector3Field(GUIContents.boundingOffset, selectedData.boundingSphereOffset);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selectedData, "Change bounding");
                selectedData.boundingSphereOffset = boundingSphereOffset;
            }
            float boundingSphereRadius = EditorGUILayout.FloatField(GUIContents.boundingSize, selectedData.boundingSphereRadius);
            if (boundingSphereRadius != selectedData.boundingSphereRadius)
            {
                Undo.RecordObject(selectedData, "Change bounding");
                selectedData.boundingSphereRadius = boundingSphereRadius;
                if (selectedData.boundingSphereRadius < 0)
                    selectedData.boundingSphereRadius = 0;
            }
            EditorGUI.indentLevel--;
        }

        private void DrawAnimations()
        {
            AnimationInfo[] anim = selectedData.animations;
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
            EditorGUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUI.enabled = !animationMode;
            if (GUILayout.Button("Animation"))
                animationMode = true;
            GUI.enabled = animationMode;
            if (GUILayout.Button("Transitions"))
                animationMode = false;
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            EditorGUI.indentLevel++;

            if (animationMode)
            {
                irb.rendererInstance.editor_transition = 1;
                irb.editor_selectedAnimation = EditorGUILayout.Popup(GUIContents.animation, irb.editor_selectedAnimation, names);
                irb.editor_autoPlayMode = EditorGUILayout.Toggle(GUIContents.autoPlay, irb.editor_autoPlayMode);
                GUI.enabled = !irb.editor_autoPlayMode;
                GUI.enabled = true;
                if (!irb.editor_autoPlayMode)
                    irb.editor_selectedAnimationFrame = EditorGUILayout.Slider(GUIContents.animationFrame, irb.editor_selectedAnimationFrame, 0, anim[irb.editor_selectedAnimation].totalFrame - 1);
                else
                    irb.editor_playbackSpeed = EditorGUILayout.FloatField(GUIContents.animationPlaybackSpeed, irb.editor_playbackSpeed);
            }
            else
            {
                irb.editor_autoPlayMode = true;
                EditorGUI.BeginChangeCheck();
                irb.editor_previousAnimation = EditorGUILayout.Popup(GUIContents.transitionFromAnim, irb.editor_previousAnimation, names);
                irb.editor_selectedAnimation = EditorGUILayout.Popup(GUIContents.transitionToAnim, irb.editor_selectedAnimation, names);
                irb.editor_playbackSpeed = EditorGUILayout.FloatField(GUIContents.animationPlaybackSpeed, irb.editor_playbackSpeed);
                InstancedRenderer.editor_transitionScale = EditorGUILayout.FloatField(GUIContents.transitionDuration, InstancedRenderer.editor_transitionScale);
                if (EditorGUI.EndChangeCheck())
                {
                    InstancedRenderer.editor_transitionUpdate = false;
                    irb.rendererInstance.editor_transition = 0;
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(GUIContents.transitionReset))
                {
                    irb.rendererInstance.editor_transition = 0;
                    irb.editor_previousAnimationFrame = 0;
                    irb.editor_selectedAnimationFrame = 0;
                    InstancedRenderer.editor_transitionUpdate = false;
                }
                GUI.enabled = !InstancedRenderer.editor_transitionUpdate;
                if (GUILayout.Button(GUIContents.transitionPlay))
                {
                    irb.rendererInstance.editor_transition = 0;
                    InstancedRenderer.editor_transitionUpdate = true;
                    irb.editor_selectedAnimationFrame = 0;
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void GUIEvents()
        {
            EventType raw = Event.current.rawType;
            if (Event.current.isMouse)
            {
                int button = Event.current.button;
                if (raw == EventType.MouseDown)
                {
                    if (textureRenderRect.Contains(Event.current.mousePosition))
                        allowMouseDrag = true;
                }
                else if (raw == EventType.MouseUp)
                    allowMouseDrag = false;
                if (raw == EventType.MouseDrag && allowMouseDrag)
                {
                    if (button == 1)
                    {
                        Vector2 offset = Event.current.delta;
                        renderCamera.transform.RotateAround(centerOfSystem, Vector3.up, offset.x);
                        Vector3 euler = renderCamera.transform.rotation.eulerAngles;
                        euler.z = 0;
                        renderCamera.transform.rotation = Quaternion.Euler(euler);

                    }
                    else if (button == 0)
                    {
                        Vector2 offset = Event.current.delta;
                        Vector3 move = new Vector3(0, offset.y * 0.01f, 0);
                        renderCamera.transform.position += move;
                        centerOfSystem += move;
                    }
                }
                else if (allowMouseDrag && Event.current.button == 2)
                {
                    ResetCamera();
                }

            }
            else if (raw == EventType.ScrollWheel && textureRenderRect.Contains(Event.current.mousePosition))
            {
                float x = -Event.current.delta.y * 0.02f;
                Vector3 normal = (centerOfSystem - renderCamera.transform.position).normalized;
                float distance = Vector3.Distance(centerOfSystem, renderCamera.transform.position);
                if (distance > 50)
                    distance = 50;
                renderCamera.transform.position += normal * x * distance;
            }
        }

        public static string OpenSaveFileWindow(string folder, string name)
        {
            string newPath = "";
            string path = EditorUtility.SaveFilePanel("Save as...", folder, name, "asset");
            if (path.Length > 0)
            {
                if (path.Contains(Application.dataPath))
                {
                    string s = path;
                    string d = Application.dataPath + "/";
                    string p = "Assets/" + s.Remove(0, d.Length);
                    newPath = p;
                }
                else
                {
                    Debug.LogError("Path is outside project: " + path);
                }
            }
            return newPath;
        }

        private void ConfigNewInstance()
        {
            ResetCamera();
            GameObject gm = new GameObject();
            gm.SetActive(false);
            gm.transform.position = centerOfSystemSpawnPoint;
            gm.hideFlags = HideFlags.HideAndDontSave;
            irb = gm.AddComponent<InstancedAnimationRenderer>();
            irb.animationData = selectedData;
            irb.editor_autoPlayMode = true;
            irb.Start();
            if (selectedData != null)
            {
                this.attachmentLOD = selectedData.LOD.Length - 1;
                this.attachmentlayerID = selectedData.LOD[0].layer[0];
            }
        }
    }
}
#endif