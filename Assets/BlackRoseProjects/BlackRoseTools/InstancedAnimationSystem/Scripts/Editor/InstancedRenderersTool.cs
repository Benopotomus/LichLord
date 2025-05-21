#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine.Jobs;
using System.Collections.Generic;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [EditorToolContext("Instanced Renderer")]
    [Icon(InstancedRenderingResources.InstancingToolIcon)]
    internal class InstancedRendererContext : EditorToolContext
    {
        internal static bool allowMultipleModification = true;
        internal static bool needDirtySelection;
        internal static bool isDuringMoving;
        internal static bool modified;
        private static List<Transform> transformArray = new List<Transform>();

        public override void OnToolGUI(EditorWindow _) { }
        protected override Type GetEditorToolType(Tool tool)
        {
            TryDirtySelection();
            switch (tool)
            {
                case Tool.Move:
                    return typeof(InstancedRenderersToolMovement);
                case Tool.Rotate:
                    return typeof(InstancedRenderersToolRotation);
                case Tool.Scale:
                    return typeof(InstancedRenderersToolScale);
                default:
                    return null;
            }
        }

        [Shortcut("Activate Instanced Rendering tool", typeof(SceneView), KeyCode.I)]
        static void InstancedRenderingToolShortcut()
        {
            if (ToolManager.activeContextType == typeof(InstancedRendererContext))
            {
                TryDirtySelection();
                ToolManager.SetActiveContext(null);
            }
            else
                ToolManager.SetActiveContext<InstancedRendererContext>();
        }

        public override void OnActivated()
        {
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Entering Instanced Rendering selection mode"), .7f);
        }

        public override void OnWillBeDeactivated()
        {
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Exiting Instanced Rendering selection mode"), .7f);
        }

        internal static void DrawDefaultGUI()
        {
            Handles.BeginGUI();
            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label("Hold Shift to select in Scene View");
                    allowMultipleModification = EditorGUILayout.Toggle("Allow merged modification", allowMultipleModification);
                    GUILayout.Label("Selected objects: " + InstancedAnimationEditorManager.selection.Count);
                }

                GUILayout.FlexibleSpace();
            }
            Handles.EndGUI();
        }

        internal static void TryDirtySelection()
        {
            if (needDirtySelection)
            {
                needDirtySelection = false;
                List<GameObject> behs = InstancedAnimationEditorManager.GetSelection();
                int size = behs.Count;
                transformArray.Clear();
                for (int i = 0; i < size; ++i)
                    if (behs[i] != null && behs[i].transform != null)
                        transformArray.Add(behs[i].transform);
                Undo.RecordObjects(transformArray.ToArray(), "Update selection");
            }
        }
    }

    internal class InstancedRenderersToolMovement : EditorTool
    {
        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView sceneView)
                return;

            InstancedRendererContext.DrawDefaultGUI();
            if (InstancedAnimationEditorManager.SceneEventUpdate(sceneView))
                return;

            List<GameObject> behs = InstancedAnimationEditorManager.GetSelection();
            int size = behs.Count;
            if (size == 0)
                return;

            if (InstancedRendererContext.allowMultipleModification)
            {
                Transform t = behs[0].transform;
                Vector3 startPos = t.position;
                EditorGUI.BeginChangeCheck();
                Vector3 start = Handles.PositionHandle(startPos, t.rotation);
                bool moving = start != startPos;
                if (EditorGUI.EndChangeCheck())
                {
                    if (moving)
                    {
                        InstancedRendererContext.TryDirtySelection();
                        TransformAccessArray arrayAccess = new TransformAccessArray(size);
                        for (int i = 0; i < size; ++i)
                            arrayAccess.Add(behs[i].transform);
                        MoveAllByOffset job = new MoveAllByOffset()
                        {
                            offset = start - startPos
                        };
                        job.Schedule(arrayAccess).Complete();
                        arrayAccess.Dispose();
                    }
                }
                else
                    InstancedRendererContext.needDirtySelection = true;
            }
            else
            {
                for (int i = 0; i < size; ++i)
                {
                    GameObject obj = behs[i];
                    Transform t = obj.transform;
                    EditorGUI.BeginChangeCheck();
                    Vector3 start = Handles.PositionHandle(t.position, t.rotation);
                    bool moving = start != t.position;
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (moving)
                        {
                            Undo.RecordObject(obj.transform, "Set Instances Destinations");
                            t.position = start;
                        }
                    }
                }
            }
        }
    }

    internal class InstancedRenderersToolRotation : EditorTool
    {
        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView sceneView)
                return;

            InstancedRendererContext.DrawDefaultGUI();
            if (InstancedAnimationEditorManager.SceneEventUpdate(sceneView))
                return;

            List<GameObject> behs = InstancedAnimationEditorManager.GetSelection();
            int size = behs.Count;
            if (size == 0)
                return;
            if (InstancedRendererContext.allowMultipleModification)
            {
                Transform t = behs[0].transform;
                Quaternion startRot = t.rotation;
                EditorGUI.BeginChangeCheck();
                Quaternion rot = Handles.RotationHandle(startRot, t.position);
                bool isMoving = rot != startRot;
                if (EditorGUI.EndChangeCheck())
                {
                    if (isMoving)
                    {
                        InstancedRendererContext.TryDirtySelection();
                        TransformAccessArray arrayAccess = new TransformAccessArray(size);
                        for (int i = 0; i < size; ++i)
                            arrayAccess.Add(behs[i].transform);
                        RotateAllByOffset job = new RotateAllByOffset()
                        {
                            offset = rot
                        };
                        job.Schedule(arrayAccess).Complete();
                        arrayAccess.Dispose();
                    }
                }
                else
                    InstancedRendererContext.needDirtySelection = true;
            }
            else
            {
                for (int i = 0; i < size; ++i)
                {
                    GameObject obj = behs[i];
                    Transform t = obj.transform;
                    EditorGUI.BeginChangeCheck();
                    Quaternion start = Handles.RotationHandle(t.rotation, t.position);
                    bool isMoving = start != t.rotation;
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (isMoving)
                        {
                            Undo.RecordObject(obj, "Set Instances Rotation");
                            t.rotation = start;
                        }
                    }
                }
            }
        }
    }

    internal class InstancedRenderersToolScale : EditorTool
    {
        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView sceneView)
                return;

            InstancedRendererContext.DrawDefaultGUI();
            if (InstancedAnimationEditorManager.SceneEventUpdate(sceneView))
                return;

            List<GameObject> behs = InstancedAnimationEditorManager.GetSelection();
            int size = behs.Count;
            if (size == 0)
                return;

            if (InstancedRendererContext.allowMultipleModification)
            {
                Transform t = behs[0].transform;
                Vector3 startScale = t.localScale;
                EditorGUI.BeginChangeCheck();
                Vector3 scale = Handles.ScaleHandle(startScale, t.position, t.rotation);
                bool isMoving = scale != startScale;
                if (EditorGUI.EndChangeCheck())
                {
                    if (isMoving)
                    {
                        InstancedRendererContext.TryDirtySelection();
                        TransformAccessArray arrayAccess = new TransformAccessArray(size);
                        for (int i = 0; i < size; ++i)
                            arrayAccess.Add(behs[i].transform);
                        ScaleAllByOffset job = new ScaleAllByOffset()
                        {
                            offset = scale - startScale
                        };
                        job.Schedule(arrayAccess).Complete();
                        arrayAccess.Dispose();
                    }
                }
                else
                    InstancedRendererContext.needDirtySelection = true;
            }
            else

                for (int i = 0; i < size; ++i)
                {
                    GameObject obj = behs[i];
                    Transform t = obj.transform;
                    EditorGUI.BeginChangeCheck();
                    Vector3 start = Handles.ScaleHandle(t.localScale, t.position, t.rotation);
                    bool isMoving = start != t.localScale;
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (isMoving)
                        {
                            Undo.RecordObject(obj, "Set Instances Scale");
                            t.localScale = start;
                        }
                    }
                }
        }
    }
}
#endif