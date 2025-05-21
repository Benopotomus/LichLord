#if BLACKROSE_INSTANCING_URP && BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class InstancedAnimationSettingsDestroyEvent : UnityEditor.AssetModificationProcessor
    {
        static Type _type = typeof(InstancedAnimationSystemSettings);

        public static UnityEditor.AssetDeleteResult OnWillDeleteAsset(string path, UnityEditor.RemoveAssetOptions _)
        {
            if (path == "Assets/BlackRoseProjects" || path == "Assets/BlackRoseProjects/BlackRoseTools" || path.StartsWith("Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem"))
            {
                InstancedAnimationSystemInitializer.UpdateURPOutline(false);
                return UnityEditor.AssetDeleteResult.DidNotDelete;
            }
            if (!path.EndsWith(".asset"))
                return UnityEditor.AssetDeleteResult.DidNotDelete;

            var assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
            if (assetType != null && (assetType == _type || assetType.IsSubclassOf(_type)))
            {
                InstancedAnimationSystemInitializer.UpdateURPOutline(false);
            }

            return UnityEditor.AssetDeleteResult.DidNotDelete;
        }
    }
}
#endif