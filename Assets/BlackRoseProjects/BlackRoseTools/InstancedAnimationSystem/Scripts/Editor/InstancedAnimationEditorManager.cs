#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Rendering;
using System.Diagnostics;
using BlackRoseProjects.Utility;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal static class InstancedRenderingResources
    {
        public const string InstancingToolIcon = "Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Icons/Icon_InstancingToolsIcon.png";
        public const string PivotModel = "Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Models/pivot.fbx";
        public const string GridTexture = "Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Textures/grid.png";
        public const string TooltipTexture = "Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Textures/MousePrompts.png";
        public const string HDRPOutlineMaterial = "Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Materials/HDRP_outlineMaterial.mat";
    }

    internal class InstancedAnimationEditorManager
    {
        internal static InstancedAnimationSystemSettings settings;
        internal static List<GameObject> selection = new List<GameObject>();
        internal static List<InstancedAnimationRenderer> selectionBeh = new List<InstancedAnimationRenderer>();
        internal static bool selectionChanged = false;
        private static List<Camera> cameras = new List<Camera>();
        private static Vector3 _editor_startDrag;
        private static int lastWasHold = -1;
#if BLACKROSE_INSTANCING_HDRP
        private static Material HDRP_OutlineMaterial;
        private static Dictionary<InstancedAnimationData, Material> HDRP_outlineMaterials = new Dictionary<InstancedAnimationData, Material>();
#endif

        //lod helpers
        private static GUIStyle richLabel;
        private static GUIStyle labelLodGroups;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.update += InternalEditorUpdate;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += SavedScene;
            EditorApplication.playModeStateChanged += PlaymodeChanged;
            SceneView.duringSceneGui += SceneViewGUI;
            Selection.selectionChanged += SelectionChanged;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            settings = InstancedAnimationSystemSettings.GetSettings();
            InstancedAnimationSystemInitializer.UpdateURPOutline();

            GlobalKeyword keyword = GlobalKeyword.Create(InstancedAnimationHelper.INSTANCING_NORMAL_TRANSITION_BLENDING);
            if (settings.transitionsBlending)
                Shader.EnableKeyword(keyword);
            else
                Shader.DisableKeyword(keyword);

        }
#if BLACKROSE_INSTANCING_HDRP
        private static Material GetHDRP_Outline(InstancedAnimationRenderer data)
        {
            if (HDRP_OutlineMaterial == null)
                HDRP_OutlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(InstancedRenderingResources.HDRPOutlineMaterial);
            if (HDRP_OutlineMaterial != null)
            {
                if (!HDRP_outlineMaterials.TryGetValue(data.animationData, out Material mat))
                    HDRP_outlineMaterials[data.animationData] = mat = GetNewOutlineMaterial();
                else if (mat == null)
                    HDRP_outlineMaterials[data.animationData] = mat = GetNewOutlineMaterial();
                data.InstancedRenderer.ConfigMaterial(mat);
                return mat;
            }
            return HDRP_OutlineMaterial;
        }

        private static Material GetNewOutlineMaterial()
        {
            Material mat = Material.Instantiate(HDRP_OutlineMaterial);
            mat.SetColor("_Color", settings.selectionOutlineColor);
            return mat;
        }
#endif

        internal static void DrawLODBar(Rect rect, InstancedAnimationData data, Vector3 position, float scale, Camera camera, bool drawInfo = true, bool drawBar = true)
        {
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (richLabel == null)
            {
                richLabel = new GUIStyle(GUI.skin.label);
                richLabel.richText = true;
                richLabel.fixedWidth = 100f;

                labelLodGroups = new GUIStyle(GUI.skin.label);
                labelLodGroups.alignment = TextAnchor.UpperLeft;
                labelLodGroups.normal.textColor = Color.white;
                labelLodGroups.fontSize = 11;
                labelLodGroups.margin = new RectOffset();
                labelLodGroups.padding = new RectOffset(2, 2, 2, 2);
            }

            float distance = camera ? (camera.transform.position - position).sqrMagnitude : 1;
            float bias = QualitySettings.lodBias;
            distance /= bias;
            int LOD = -1;
            float ratio = 1;
            float cumulated = 0;
            float maxRange = data.LOD[data.LOD.Length - 1].height;
            bool hasInfinity = false;
            if (maxRange < 0)
            {
                hasInfinity = true;
            }
            maxRange = /*camera ? camera.farClipPlane : */10000;
            maxRange *= maxRange;
            float distLog = math.log2(distance + 1);
            float maxDistLog = math.log2(maxRange);

            for (int i = 0; i < data.LOD.Length; ++i)
            {
                InstancingLODData lod = data.LOD[i];

                float l = lod.height * scale;
                if (l > 0)
                {
                    l *= scale;
                    l *= l;
                }
                else
                {
                    LOD = i;
                    ratio = 1;
                    break;
                }
                if (l > distance)
                {
                    LOD = i;

                    ratio = math.clamp((distance - cumulated) / (l - cumulated), 0f, 1f);
                    break;
                }
                cumulated = l;
            }
            if (drawInfo)
                EditorGUILayout.LabelField($"<b>LOD</b>: {LOD} [{(ratio * 100f):0.0}%]", richLabel);

            float fullSize = rect.width;
            Color[] colors = new Color[5];
            colors[0] = new Color(97f / 255f, 125f / 255f, 5f / 255f);
            colors[1] = new Color(45f / 255f, 55f / 255f, 67f / 255f);
            colors[2] = new Color(40f / 255f, 64f / 255f, 73f / 255f);
            colors[3] = new Color(64f / 255f, 37f / 255f, 27f / 255f);
            colors[4] = new Color(81 / 255f, 0, 0);
            Color pointer = Color.black;

            float lastEndAt = 0;

            for (int i = 0; i < data.LOD.Length; ++i)
            {
                InstancingLODData ilod = data.LOD[i];
                float dist = ilod.height;
                if (dist == -1)
                    dist = maxRange;
                else
                    dist *= scale;
                dist *= dist;
                float log = math.log2(dist + 1);

                Rect current = new Rect(rect.x + lastEndAt, rect.y, fullSize * (log / maxDistLog) - lastEndAt, rect.height);
                EditorGUI.DrawRect(current, colors[i]);
                EditorGUI.LabelField(current, "LOD " + i, labelLodGroups);
                if (ilod.height == -1)
                    EditorGUI.LabelField(new Rect(current.x, current.y + 10, current.width, current.height), "[infinity]", labelLodGroups);
                else
                    EditorGUI.LabelField(new Rect(current.x, current.y + 10, current.width, current.height), $"[{ilod.height:0.00}m]", labelLodGroups);
                lastEndAt = fullSize * (log / maxDistLog);
            }
            if (!hasInfinity)
            {
                Rect current = new Rect(rect.x + lastEndAt, rect.y, fullSize - lastEndAt, rect.height);
                EditorGUI.DrawRect(current, colors[colors.Length - 1]);
                EditorGUI.LabelField(current, "Culled", labelLodGroups);
            }
            if (drawBar)
                EditorGUI.DrawRect(new Rect(rect.x + (distLog / maxDistLog) * fullSize, rect.y - 1, 2, rect.height + 2), pointer);
            if (drawInfo)
                EditorGUILayout.LabelField($"Distance to camera: {math.sqrt(distance):0.0}");
            EditorGUI.indentLevel = indentLevel;
        }

        internal static List<GameObject> GetSelection()
        {
            if (selectionChanged)
            {
                selectionChanged = false;
                selection.Clear();
                selectionBeh.Clear();
                InstancedAnimationRenderer[] behs2 = Selection.GetFiltered<InstancedAnimationRenderer>(SelectionMode.Unfiltered);
                for (int i = 0; i < behs2.Length; ++i)
                {
                    selection.Add(behs2[i].gameObject);
                    selectionBeh.Add(behs2[i]);
                }
            }
            return selection;
        }

        internal static List<InstancedAnimationRenderer> GetSelectionBehaviour()
        {
            if (selectionChanged)
            {
                selectionChanged = false;
                selection.Clear();
                selectionBeh.Clear();
                InstancedAnimationRenderer[] behs2 = Selection.GetFiltered<InstancedAnimationRenderer>(SelectionMode.Unfiltered);
                for (int i = 0; i < behs2.Length; ++i)
                {
                    selection.Add(behs2[i].gameObject);
                    selectionBeh.Add(behs2[i]);
                }
            }
            return selectionBeh;
        }

        private static void SelectionChanged()
        {
            selectionChanged = true;
        }

        private static void PlaymodeChanged(PlayModeStateChange mode)
        {
            selectionChanged = true;
            selection.Clear();
            selectionBeh.Clear();
            if (mode == PlayModeStateChange.ExitingPlayMode)
                Selection.activeObject = null;
        }

        private static GameObject draggedObjViewGui;

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            SceneViewGUI(null);
        }

        private static void SceneViewGUI(SceneView view)
        {
            EventType eventType = Event.current.type;
            if ((eventType == EventType.DragUpdated || eventType == EventType.DragPerform || eventType == EventType.DragExited) && DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is InstancedAnimationData)
            {
                InstancedAnimationData iad = DragAndDrop.objectReferences[0] as InstancedAnimationData;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (eventType == EventType.DragExited)
                {
                    if (draggedObjViewGui != null)
                        Object.DestroyImmediate(draggedObjViewGui);
                    draggedObjViewGui = null;
                    return;
                }

                if (draggedObjViewGui == null && eventType == EventType.DragUpdated)
                {
                    draggedObjViewGui = new GameObject(iad.name);
                    draggedObjViewGui.hideFlags = HideFlags.HideAndDontSave;
                    draggedObjViewGui.SetActive(false);
                    draggedObjViewGui.AddComponent<InstancedAnimationRenderer>().animationData = iad;
                    if (view != null)
                        draggedObjViewGui.SetActive(true);
                    //   else
                    //       DragAndDrop.AddDropHandler(HierarhyDropDelegate);

                }
                if (view != null)
                {
                    Ray mouseRay = Camera.current.ScreenPointToRay(new Vector3(Event.current.mousePosition.x, Screen.height - Event.current.mousePosition.y, 0.0f));
                    if (mouseRay.direction.y < 0.0f)
                    {
                        float t = -mouseRay.origin.y / mouseRay.direction.y;
                        Vector3 mouseWorldPos = mouseRay.origin + t * mouseRay.direction;
                        mouseWorldPos.y = 0.0f;
                        draggedObjViewGui.transform.position = mouseWorldPos;
                    }
                }

                if (eventType == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    draggedObjViewGui.SetActive(true);
                    draggedObjViewGui.hideFlags = HideFlags.None;
                    //  DragAndDrop.RemoveDropHandler(HierarhyDropDelegate);
                    if (view == null)
                    {
                        // UnityEngine.Debug.LogError(Selection.activeTransform);
                        //   draggedObjViewGui.transform.position = new Vector3();
                    }
                    // EditorGUIUtility.hie
                    Undo.RegisterCreatedObjectUndo(draggedObjViewGui, "Drag");
                    EditorGUIUtility.PingObject(draggedObjViewGui);
                    //  Selection.activeObject = draggedObjViewGui;
                    draggedObjViewGui = null;
                }
                Event.current.Use();
            }

            if (!EditorApplication.isPlaying || InstancedAnimationManager.instance == null)
                return;

            int aliveInstances = InstancedAnimationManager.instance.instancedRenderersList.Count;
            if (aliveInstances == 0 || !EditorApplication.isPaused || !settings.renderInstancesDuringPause)
                return;
            SceneView.RepaintAll();
            EditorUtility.SetDirty(InstancedAnimationManager.instance);
            InstancedAnimationManager.instance.InternalUpdate();
        }

        private static DragAndDropVisualMode HierarhyDropDelegate(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            return DragAndDropVisualMode.Copy;
        }

        /// <summary>
        /// Provide selection GUI events
        /// </summary>
        /// <param name="view"></param>
        /// <returns>return false if not holding shift</returns>
        internal static bool SceneEventUpdate(SceneView view)
        {
            Event e = Event.current;
            if (!e.shift)
                return false;
            if (e.type == EventType.Repaint || e.type == EventType.Layout || e.type == EventType.KeyDown)
                return true;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                lastWasHold = 0;
                _editor_startDrag = e.mousePosition;
                float ppp = EditorGUIUtility.pixelsPerPoint;
                _editor_startDrag.y = view.camera.pixelHeight - _editor_startDrag.y * ppp;
                _editor_startDrag.x *= ppp;
                ClickGuiEvent(view, e);
                return true;
            }
            if (lastWasHold >= 0)
            {

                if (e.type == EventType.Used || e.type == EventType.MouseDrag)
                {
#if !UNITY_2022_1_OR_NEWER
                    if (lastWasHold == 0)
                        ClickGuiEvent(view, e);
                    else
#endif
                    DragGuiEvent(view, e);
                    ++lastWasHold;
                }
                else
                    lastWasHold = -1;
            }
            return true;
        }

        private static void DragGuiEvent(SceneView view, Event e)
        {
            Vector3 mousePos = e.mousePosition;
            float ppp = EditorGUIUtility.pixelsPerPoint;
            mousePos.y = view.camera.pixelHeight - mousePos.y * ppp;
            mousePos.x *= ppp;

            if (math.distance(mousePos, _editor_startDrag) < 4)
                return;
            int objects;
            bool staticMode = false;
            if (Application.isPlaying && InstancedAnimationManager.instance != null)
            {
                objects = InstancedAnimationManager.instance.instancedRenderersList.Count;
            }
            else
            {
                staticMode = true;
                objects = InstancedAnimationRenderer.editor_behaviours.Count;

            }
            selection.Clear();
            selectionBeh.Clear();
            if (objects == 0)
                return;

            Vector3 tmp1 = new Vector3(_editor_startDrag.x, mousePos.y);
            Vector3 tmp2 = new Vector3(mousePos.x, _editor_startDrag.y);

            Ray ray1 = view.camera.ScreenPointToRay(_editor_startDrag);//left top
            Ray ray2 = view.camera.ScreenPointToRay(mousePos);//right bot
            Ray ray3 = view.camera.ScreenPointToRay(tmp1);//left bot
            Ray ray4 = view.camera.ScreenPointToRay(tmp2);//right top

            //by default click starts at left top and go right down
            //left top is 0,0, right bottom is +x,+y

            if (mousePos.x < _editor_startDrag.x && mousePos.y < _editor_startDrag.y)
            {//mouse is on left, swich sides

                Ray tmp = ray1;
                ray1 = ray4;
                ray4 = tmp;

                tmp = ray2;
                ray2 = ray3;
                ray3 = tmp;
            }

            if (mousePos.y > _editor_startDrag.y && mousePos.x > _editor_startDrag.x)
            {//mouse is above, switch heights

                Ray tmp = ray1;
                ray1 = ray3;
                ray3 = tmp;

                tmp = ray2;
                ray2 = ray4;
                ray4 = tmp;
            }

            NativeArray<bool> output = new NativeArray<bool>(objects, Allocator.TempJob);

            float3 a = ray1.origin;
            float3 b = ray2.origin;
            float3 c = ray3.origin;
            float3 d = ray4.origin;

            float3 a2 = ray1.origin + ray1.direction * 1000;
            float3 b2 = ray2.origin + ray2.direction * 1000;
            float3 c2 = ray3.origin + ray3.direction * 1000;
            float3 d2 = ray4.origin + ray4.direction * 1000;


            Plane[] planes = new Plane[6];
            planes[0] = new Plane(a, a2, c);
            planes[1] = new Plane(a, d, a2);
            planes[2] = new Plane(d, b2, d2);
            planes[3] = new Plane(b, c, b2);
            planes[4] = new Plane(a2, d2, c2);
            planes[5] = new Plane(a, c, d);

            float4[] frustumPlanes = new float4[6];
            for (int i = 0; i < 6; i++)
            {
                Vector3 normal = planes[i].normal;
                frustumPlanes[i] = new float4(normal.x, normal.y, normal.z, planes[i].distance);
            }

            if (staticMode)
            {
                NativeArray<Matrix4x4> matrice = new NativeArray<Matrix4x4>(objects, Allocator.TempJob);
                NativeArray<float> sizes = new NativeArray<float>(objects, Allocator.TempJob);
                NativeArray<float3> offsets = new NativeArray<float3>(objects, Allocator.TempJob);

                for (int i = 0; i < objects; ++i)
                {
                    InstancedAnimationRenderer irb = InstancedAnimationRenderer.editor_behaviours[i];
                    matrice[i] = irb.transform.localToWorldMatrix;
                    sizes[i] = irb.animationData.boundingSphereRadius;
                    offsets[i] = irb.animationData.boundingSphereOffset;
                }

                JobHandle job = new RaycastJobSelection()
                {
                    p0 = frustumPlanes[0],
                    p1 = frustumPlanes[1],
                    p2 = frustumPlanes[2],
                    p3 = frustumPlanes[3],
                    p4 = frustumPlanes[4],
                    p5 = frustumPlanes[5],
                    matrix = matrice,
                    unscaledSized = sizes,
                    offsets = offsets,
                    output = output
                }.Schedule(objects, 32);
                job.Complete();

                matrice.Dispose();
                sizes.Dispose();
                offsets.Dispose();

                for (int i = 0; i < output.Length; ++i)
                {
                    if (output[i])
                    {
                        selection.Add(InstancedAnimationRenderer.editor_behaviours[i].gameObject);
                        selectionBeh.Add(InstancedAnimationRenderer.editor_behaviours[i]);
                    }
                }
            }
            else
            {
                JobHandle job = new RaycastJobSelection()
                {
                    p0 = frustumPlanes[0],
                    p1 = frustumPlanes[1],
                    p2 = frustumPlanes[2],
                    p3 = frustumPlanes[3],
                    p4 = frustumPlanes[4],
                    p5 = frustumPlanes[5],
                    matrix = InstancedAnimationManager.instance.localToWorldMatrix_native,
                    unscaledSized = InstancedAnimationManager.instance.sizes_native,
                    offsets = InstancedAnimationManager.instance.offsets_native,
                    output = output
                }.Schedule(objects, 32);
                job.Complete();
                for (int i = 0; i < output.Length; ++i)
                {
                    if (output[i])
                    {
                        if (InstancedAnimationManager.instance.instancedRenderersList[i].hasTransform)
                        {
                            selection.Add(InstancedAnimationManager.instance.instancedRenderersList[i].transformReference.gameObject);
                            selectionBeh.Add(InstancedAnimationManager.instance.instancedRenderersList[i].transformReference.GetComponent<InstancedAnimationRenderer>());
                        }
                    }

                }
            }
            output.Dispose();
            Selection.objects = selection.ToArray();
            selectionChanged = false;
        }

        private static void ClickGuiEvent(SceneView view, Event e)
        {
            Vector3 mousePos = e.mousePosition;
            float ppp = EditorGUIUtility.pixelsPerPoint;
            mousePos.y = view.camera.pixelHeight - mousePos.y * ppp;
            mousePos.x *= ppp;
            _editor_startDrag = mousePos;
            Ray ray = view.camera.ScreenPointToRay(mousePos);
            int count;
            bool staticMode = false;
            if (Application.isPlaying && InstancedAnimationManager.instance != null)
                count = InstancedAnimationManager.instance.instancedRenderersList.Count;
            else
            {
                staticMode = true;
                count = InstancedAnimationRenderer.editor_behaviours.Count;
            }
            selection.Clear();
            selectionBeh.Clear();
            if (count == 0)
                return;

            NativeArray<int> output = new NativeArray<int>(Unity.Jobs.LowLevel.Unsafe.JobsUtility.MaxJobThreadCount, Allocator.TempJob);
            for (int i = 0; i < output.Length; ++i)
                output[i] = -1;
            NativeArray<Matrix4x4> matrice;
            NativeArray<float> sizes;
            NativeArray<float3> offsets;
            if (staticMode)
            {
                matrice = new NativeArray<Matrix4x4>(count, Allocator.TempJob);
                sizes = new NativeArray<float>(count, Allocator.TempJob);
                offsets = new NativeArray<float3>(count, Allocator.TempJob);

                for (int i = 0; i < count; ++i)
                {
                    InstancedAnimationRenderer irb = InstancedAnimationRenderer.editor_behaviours[i];
                    matrice[i] = irb.transform.localToWorldMatrix;
                    sizes[i] = irb.animationData.boundingSphereRadius;
                    offsets[i] = irb.animationData.boundingSphereOffset;
                }

                JobHandle job = new RaycastJob()
                {
                    destination = ray.origin + ray.direction * 10000,//10000 is big enought to catch all hit instances
                    origin = ray.origin,
                    matrix = matrice,
                    unscaledSized = sizes,
                    offsets = offsets,
                    output = output
                }.Schedule(count, 32);
                job.Complete();

                matrice.Dispose();
                sizes.Dispose();
                offsets.Dispose();
            }
            else
            {
                JobHandle job = new RaycastJob()
                {
                    destination = ray.origin + ray.direction * 10000,//10000 is big enought to catch all hit instances
                    origin = ray.origin,
                    matrix = InstancedAnimationManager.instance.localToWorldMatrix_native,
                    unscaledSized = InstancedAnimationManager.instance.sizes_native,
                    offsets = InstancedAnimationManager.instance.offsets_native,
                    output = output
                }.Schedule(count, 32);
                job.Complete();
            }
            for (int i = 0; i < output.Length; ++i)
            {
                int hit = output[i];
                if (hit != -1)
                {
                    if (staticMode)
                    {
                        Selection.activeObject = null;
                        Selection.activeObject = InstancedAnimationRenderer.editor_behaviours[hit].gameObject;
                        selection.Add(InstancedAnimationRenderer.editor_behaviours[hit].gameObject);
                        selectionBeh.Add(InstancedAnimationRenderer.editor_behaviours[hit]);
                        selectionChanged = false;
                        e.Use();
                        break;
                    }
                    else if (InstancedAnimationManager.instance.instancedRenderersList[hit].hasTransform)
                    {
                        Selection.activeObject = null;
                        Selection.activeObject = InstancedAnimationManager.instance.instancedRenderersList[hit].transformReference.gameObject;
                        selection.Add(InstancedAnimationManager.instance.instancedRenderersList[hit].transformReference.gameObject);
                        selectionBeh.Add(InstancedAnimationManager.instance.instancedRenderersList[hit].transformReference.GetComponent<InstancedAnimationRenderer>());
                        selectionChanged = false;
                        e.Use();
                        break;
                    }
                    break;
                }
            }
            output.Dispose();
        }

        private static void SavedScene(UnityEngine.SceneManagement.Scene scene)
        {
            RefreshMaterials();
        }

        [MenuItem("Tools/Black Rose Projects/Instanced Animation System/Refresh materials in scene", priority = 500)]
        internal static void RefreshMaterials()
        {
            for (int i = 0; i < InstancedAnimationRenderer.editor_behaviours.Count; ++i)
            {//refresh
                InstancedAnimationRenderer irb = InstancedAnimationRenderer.editor_behaviours[i];
                if (irb.rendererInstance != null)
                    irb.rendererInstance.EditorOnly_Initialize(irb.transform);
            }
        }

        internal static void RenderOutlineEditorMode(CommandBuffer cb)
        {
            List<InstancedAnimationRenderer> objects = GetSelectionBehaviour();
            for (int i = 0, size = objects.Count; i < size; ++i)
                objects[i].EditorOnly_RenderOutline(cb);
        }

        internal static void RenderOutlinePlayMode(CommandBuffer cb)
        {
            if (InstancedAnimationManager.instance != null && InstancedAnimationManager.instance.CurrentCamera != null)
            {
                List<InstancedAnimationRenderer> objects = GetSelectionBehaviour();
                for (int i = 0, size = objects.Count; i < size; ++i)
                    objects[i].EditorOnly_RenderOutlinePlayMode(cb);
            }
        }

        private static bool lastWasActive = false;
        private static float lastTime;
        private static Stopwatch watch;
        internal static float EditorDeltaTime;//this editor delta time is to fix SRP scene updating time while without play mode

        internal static void InternalEditorUpdate()
        {
            Pipelines usingPipeline = BRPPipelineHelper.GetCurrentPipeline();
            if (!Application.isPlaying && usingPipeline != Pipelines.Built_In)
            {
                if (watch == null)
                {
                    watch = Stopwatch.StartNew();
                    lastTime = 0;
                }
                else
                {
                    float current = (float)watch.Elapsed.TotalSeconds;
                    EditorDeltaTime = current - lastTime;
                    lastTime = current;
                }
            }
            else
                EditorDeltaTime = Time.deltaTime;

            InstancedAnimationEditorOutline.onRenderOutline = Application.isPlaying ? RenderOutlinePlayMode : RenderOutlineEditorMode;

            if (UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            {
                if (!lastWasActive)
                {
                    lastWasActive = true;
                    RefreshMaterials();
                }
            }
            else
            {
                lastWasActive = false;
                return;
            }


            int behavioursCount = InstancedAnimationRenderer.editor_behaviours.Count;
            if (behavioursCount == 0 && !InstancedRendererConfigurator.isVisable)
                return;
            Camera lodCamera = null;
            bool allowRendering = Application.isPlaying || settings.editorRenderMode == InstancedAnimationSystemSettings.EditorRenderMode.renderFull || InstancedRendererConfigurator.isVisable;
            if (!allowRendering && settings.editorRenderMode == InstancedAnimationSystemSettings.EditorRenderMode.onlySelected)
            {
                List<InstancedAnimationRenderer> selection = GetSelectionBehaviour();
                if (selection.Count > 0)
                    allowRendering = true;
            }


            if (allowRendering)
            {
                cameras.Clear();
                if (!Application.isPlaying)
                {
                    Camera main = Camera.main;
                    if (main != null)
                        cameras.Add(main);
                }
                var s = SceneView.sceneViews;

                foreach (var element in s)
                {
                    SceneView sv = (SceneView)element;

                    Camera c = sv.camera;
                    if (c != null)
                    {
                        InstancedAnimationEditorOutline ceo = c.GetComponent<InstancedAnimationEditorOutline>();
                        if (ceo == null)
                            ceo = c.gameObject.AddComponent<InstancedAnimationEditorOutline>();
                        bool isBuildIn = usingPipeline == Pipelines.Built_In;

                        //    if (ceo.wasActive || !isBuildIn)
                        //    {
                        if (!isBuildIn)
                            sv.Repaint();
                        EditorUtility.SetDirty(sv);
                        cameras.Add(c);
                        if (lodCamera == null)
                            lodCamera = c;
                        //  }
                    }
                }
            }
            else
                InstancedAnimationEditorOutline.DestroyAll();

            if (usingPipeline == Pipelines.HDRP && Application.isPlaying)
                HDRPOutline();

            if (behavioursCount == 0 || Application.isPlaying || !allowRendering || !UnityEditorInternal.InternalEditorUtility.isApplicationActive || cameras.Count == 0)
                return;

            if (lodCamera == null)
                lodCamera = cameras[0];
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (settings.editorRenderMode == InstancedAnimationSystemSettings.EditorRenderMode.onlySelected)
            {
                List<InstancedAnimationRenderer> selection = GetSelectionBehaviour();
                if (prefabStage == null)
                {
                    for (int i = 0; i < selection.Count; ++i)
                        selection[i].EditorOnly_Update(cameras, lodCamera, EditorDeltaTime);
                }
                else
                {
                    for (int i = 0; i < selection.Count; ++i)
                    {
                        InstancedAnimationRenderer irb = selection[i];
                        if (prefabStage.IsPartOfPrefabContents(irb.gameObject))
                            irb.EditorOnly_Update(cameras, lodCamera, EditorDeltaTime);
                    }
                }
            }
            else
            {
                if (prefabStage == null)
                {
                    for (int i = 0; i < behavioursCount; ++i)
                        InstancedAnimationRenderer.editor_behaviours[i].EditorOnly_Update(cameras, lodCamera, EditorDeltaTime);
                }
                else
                {
                    for (int i = 0; i < behavioursCount; ++i)
                    {
                        InstancedAnimationRenderer irb = InstancedAnimationRenderer.editor_behaviours[i];
                        if (prefabStage.IsPartOfPrefabContents(irb.gameObject))
                            irb.EditorOnly_Update(cameras, lodCamera, EditorDeltaTime);
                    }
                }
            }
            if (usingPipeline == Pipelines.HDRP)
                HDRPOutline();

        }

        private static void HDRPOutline()
        {
#if BLACKROSE_INSTANCING_HDRP
            if (settings.selectionOutlineActive)
            {
                List<Camera> cams = new List<Camera>();
                foreach (var element in SceneView.sceneViews)
                {
                    SceneView sv = (SceneView)element;

                    Camera c = sv.camera;
                    cams.Add(c);
                }
                if (cams.Count == 0)
                    return;
                List<InstancedAnimationRenderer> selection = GetSelectionBehaviour();
                int behavioursCount = selection.Count;

                for (int i = 0; i < behavioursCount; ++i)
                {
                    var instance = selection[i];
                    if (instance == null || instance.rendererInstance == null)
                        continue;
                    Material mat = GetHDRP_Outline(instance);
                    instance.EditorOnly_RenderOutline(cams, mat);
                }
            }
#endif
        }
    }
}
#endif