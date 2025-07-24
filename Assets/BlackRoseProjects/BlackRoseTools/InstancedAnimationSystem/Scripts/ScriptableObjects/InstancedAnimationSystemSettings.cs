using System;
using UnityEngine;
namespace BlackRoseProjects.InstancedAnimationSystem
{
    [HelpURL("http://docs.blackrosetools.com/InstancedAnimations/html/index.html")]
    internal class InstancedAnimationSystemSettings : ScriptableObject
    {
        internal const string settingsFullPath = "Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Resources/InstancingSettings.asset";
        internal const string settingsPath = "InstancingSettings";
        internal const int maxInstancesPerBatch = 511;

        [Serializable] public enum EditorRenderMode { renderFull, onlySelected, onlyGizmosSelected, none }

        [SerializeField, Range(1, maxInstancesPerBatch)] internal int instancingPackageSize;
        [SerializeField] internal int maxInstancedObjects;
        [SerializeField] internal bool initAtStartup;
        [SerializeField] internal bool renderOnlyToCurrentCamera;
        [SerializeField] internal bool renderInstancesDuringPause;
        [SerializeField] internal bool selectionOutlineActive;
        [SerializeField] internal Color selectionOutlineColor;
        [SerializeField] internal bool drawBoundingGizmoSpheres;
        [SerializeField] internal EditorRenderMode editorRenderMode;
        [SerializeField] internal bool transitionsBlending;

        internal void FillDefaultValues()
        {
            instancingPackageSize = maxInstancesPerBatch;
            maxInstancedObjects = 40000;
            renderInstancesDuringPause = true;
            renderOnlyToCurrentCamera = false;
            initAtStartup = true;
            editorRenderMode = EditorRenderMode.renderFull;
            drawBoundingGizmoSpheres = false;
            transitionsBlending = true;
            selectionOutlineColor = new Color(1, 0.4f, 0, 0);
            selectionOutlineActive = true;
        }

        internal static InstancedAnimationSystemSettings GetSettings()
        {
            InstancedAnimationSystemSettings settings = Resources.Load<InstancedAnimationSystemSettings>(settingsPath);
            if (settings == null)
            {
                Debug.Log("Creating temporary Instancing settings!");
                settings = CreateInstance<InstancedAnimationSystemSettings>();
                settings.FillDefaultValues();
            }
            return settings;
        }
    }
}