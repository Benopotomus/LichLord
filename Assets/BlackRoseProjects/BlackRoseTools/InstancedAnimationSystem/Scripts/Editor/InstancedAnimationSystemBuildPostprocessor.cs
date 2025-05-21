#if BLACKROSE_INSTANCING_URP && BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class InstancedAnimationSystemBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 32000; } }

        public void OnPostprocessBuild(BuildReport report)
        {//add URP outline after build
            InstancedAnimationSystemInitializer.UpdateURPOutline(true);
        }
    }
}
#endif