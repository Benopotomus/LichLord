using UnityEngine;
using UnityEditor;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class InstancedAnimationWelcomeWindow : EditorWindow
    {
        private static InstancedAnimationWelcomeWindow window;
#if !BLACKROSE_INSTANCING_COLLECTIONS || !BLACKROSE_INSTANCING_MATH || !BLACKROSE_INSTANCING_BURST

        [MenuItem("Tools/Black Rose Projects/Instanced Animation System/Install required Packages")]
        internal static void OpenIfNoPackages()
        {
            MakeWindow();
        }
#endif

        internal static void MakeWindow()
        {
            bool hasMath = InstancedAnimationHelper.HasDefinition(InstancedAnimationHelper.BLACKROSE_INSTANCING_MATH);
            bool hasCollections = InstancedAnimationHelper.HasDefinition(InstancedAnimationHelper.BLACKROSE_INSTANCING_COLLECTIONS);
            bool hasBurst = InstancedAnimationHelper.HasDefinition(InstancedAnimationHelper.BLACKROSE_INSTANCING_BURST);
            if (!(hasMath && hasCollections && hasBurst))
            {
                window = GetWindow<InstancedAnimationWelcomeWindow>("Instanced Animation System");
                window.minSize = new Vector2(512, 80);
                window.maxSize = new Vector2(512, 80);
            }
            else
            {
                if (window != null)
                    window.Close();
            }
        }

        private void OnGUI()
        {
            bool hasMath = InstancedAnimationHelper.HasDefinition(InstancedAnimationHelper.BLACKROSE_INSTANCING_MATH);
            bool hasCollections = InstancedAnimationHelper.HasDefinition(InstancedAnimationHelper.BLACKROSE_INSTANCING_COLLECTIONS);
            bool hasBurst = InstancedAnimationHelper.HasDefinition(InstancedAnimationHelper.BLACKROSE_INSTANCING_BURST);
            if (!hasMath && !hasCollections)
            {
                EditorGUILayout.HelpBox("Instanced Animation System require unity.mathematics and unity.collections to work. You can install them manually at PackageManager or click button below to automatically instal them.", MessageType.Error);
                if (GUILayout.Button("Install unity.collections and unity.mathematics"))
                {
                    Utility.BRPPackageHelper.InstallPackages(new string[] { "com.unity.collections@1.2.4", "com.unity.mathematics" });
                    Repaint();
                }
                return;
            }
            else if (!hasMath)
            {
                EditorGUILayout.HelpBox("Instanced Animation System require unity.mathematics and unity.collections to work. You can install them manually at PackageManager or click button below to automatically instal them.", MessageType.Error);
                if (GUILayout.Button("Install unity.mathematics"))
                {
                    Utility.BRPPackageHelper.InstallPackages(new string[] { "com.unity.mathematics" });
                    Repaint();
                }
                return;
            }
            else if (!hasCollections)
            {
                EditorGUILayout.HelpBox("Instanced Animation System require unity.mathematics and unity.collections to work. You can install them manually at PackageManager or click button below to automatically instal them.", MessageType.Error);
                if (GUILayout.Button("Install unity.collections"))
                {
                    Utility.BRPPackageHelper.InstallPackages(new string[] { "com.unity.collections@1.2.4" });
                    Repaint();
                }
                return;
            }
            if (!hasBurst)
            {
                EditorGUILayout.HelpBox("Instanced Animation System can work faster while using unity.burst. You can install them manually at PackageManager or click button below to automatically instal them.", MessageType.Warning);
                if (GUILayout.Button("Install unity.burst"))
                {
                    Utility.BRPPackageHelper.InstallPackages(new string[] { "com.unity.burst" });
                    Repaint();
                }
            }

            else if (hasMath && hasCollections && hasBurst)
            {
                Close();
            }
        }
    }
}