#if BLACKROSE_INSTANCING_URP && BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class InstancedAnimationSystemBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {//remove editor URP outline from build
            InstancedAnimationSystemInitializer.UpdateURPOutline(false);
        }
    }
}
#endif