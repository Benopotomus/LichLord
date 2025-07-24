using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    [InitializeOnLoad]
    internal static class InstancedAnimationSystemInitializer
    {
        private const string fisrtRun = "BRP_run";

        static InstancedAnimationSystemInitializer()
        {
            EditorApplication.update += RunOnce;
            EditorApplication.quitting += Quit;
        }

        private static void RunOnce()
        {
            EditorApplication.update -= RunOnce;
#if BLACKROSE_INSTANCING_URP
            UpdateURPOutline();
#endif
            if (!EditorPrefs.HasKey(Application.productName + fisrtRun))
            {
                EditorPrefs.SetBool(Application.productName + fisrtRun, true);
                InstancedAnimationWelcomeWindow.MakeWindow();
            }
        }

        private static void Quit()
        {
            EditorPrefs.DeleteKey(Application.productName + fisrtRun);
        }

        internal static void HideGizmoIcons()
        {
#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
            MethodInfo SetIconEnabled = Assembly.GetAssembly(typeof(Editor))?.GetType("UnityEditor.AnnotationUtility")?.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);
            const int MonoBehaviourID = 114;
            SceneView.RepaintAll();
            HideGizmoForBehaviour(typeof(InstancedAnimationRenderer), MonoBehaviourID, SetIconEnabled);
            HideGizmoForBehaviour(typeof(InstancedAnimationAttachmentBehaviour), MonoBehaviourID, SetIconEnabled);
            HideGizmoForBehaviour(typeof(InstancedAnimationBoneSyncBehaviour), MonoBehaviourID, SetIconEnabled);
#endif
        }

        private static void HideGizmoForBehaviour(Type type, int id, MethodInfo method)
        {
            method.Invoke(null, new object[] { id, type.Name, 0 });
        }

        internal static void UpdateURPOutline(bool add = true)
        {
#if BLACKROSE_INSTANCING_URP && UNITY_EDITOR
            UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset asset = (QualitySettings.renderPipeline == null ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline : QualitySettings.renderPipeline) as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (asset != null)
            {
                SerializedObject serializedObject = new SerializedObject(asset);
                serializedObject.Update();
                var renderers = serializedObject.FindProperty("m_RendererDataList");
                for (int i = 0; i < renderers.arraySize; ++i)
                {
                    SerializedProperty sp = renderers.GetArrayElementAtIndex(i);
                    if (add)
                        AddURPOutline(sp.objectReferenceValue);
                    else
                        RemoveURPOutline(sp.objectReferenceValue);
                }
            }
#endif
        }

#if BLACKROSE_INSTANCING_URP && UNITY_EDITOR



        private static void AddURPOutline(UnityEngine.Object asset)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();
            var m_RendererFeatures = serializedObject.FindProperty("m_RendererFeatures");
            var m_RendererFeaturesMap = serializedObject.FindProperty("m_RendererFeatureMap");
            for (int i = 0; i < m_RendererFeatures.arraySize; ++i)
            {
                SerializedProperty sp = m_RendererFeatures.GetArrayElementAtIndex(i);
                if (sp.objectReferenceValue.name == "InstancedAnimationEditorOutlinePostProcessRendererURP" || sp.objectReferenceValue.name == "InstancedAnimationEditorOutlinePassURP")
                    return;
            }
            ScriptableObject component = ScriptableObject.CreateInstance("InstancedAnimationEditorOutlinePostProcessRendererURP");
            component.name = "InstancedAnimationEditorOutlinePostProcessRendererURP";
            Undo.RegisterCreatedObjectUndo(component, "Add Renderer Feature");
            if (EditorUtility.IsPersistent(asset))
                AssetDatabase.AddObjectToAsset(component, asset);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long localId);

            m_RendererFeatures.arraySize++;
            SerializedProperty componentProp = m_RendererFeatures.GetArrayElementAtIndex(m_RendererFeatures.arraySize - 1);
            componentProp.objectReferenceValue = component;

            // Update GUID Map
            m_RendererFeaturesMap.arraySize++;
            SerializedProperty guidProp = m_RendererFeaturesMap.GetArrayElementAtIndex(m_RendererFeaturesMap.arraySize - 1);
            guidProp.longValue = localId;
            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            if (EditorUtility.IsPersistent(asset))
                EditorUtility.SetDirty(asset);
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static void RemoveURPOutline(UnityEngine.Object asset)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();
            var m_RendererFeatures = serializedObject.FindProperty("m_RendererFeatures");
            for (int i = 0; i < m_RendererFeatures.arraySize; ++i)
            {
                SerializedProperty sp = m_RendererFeatures.GetArrayElementAtIndex(i);
                if (sp.objectReferenceValue.name == "InstancedAnimationEditorOutlinePostProcessRendererURP" || sp.objectReferenceValue.name == "InstancedAnimationEditorOutlinePassURP")
                {
                    m_RendererFeatures.DeleteArrayElementAtIndex(i);
                    if (EditorUtility.IsPersistent(asset))
                        EditorUtility.SetDirty(asset);
                    serializedObject.ApplyModifiedProperties();
                    AssetDatabase.SaveAssetIfDirty(asset);
                    return;
                }
            }
        }
#endif
    }
}