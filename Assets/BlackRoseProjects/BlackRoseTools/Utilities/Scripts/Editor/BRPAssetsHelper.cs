using UnityEditor;
using UnityEngine;

namespace BlackRoseProjects.Utility
{
    internal static class BRPAssetsHelper
    {
        public static T CreateAsset<T>(T obj, string path) where T : Object
        {
            Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(object));
            if (asset == null)
            {
                AssetDatabase.CreateAsset(obj, path);
                return obj;
            }
            else if (asset.GetType() != obj.GetType())
            {
                AssetDatabase.MoveAssetToTrash(path);
                AssetDatabase.CreateAsset(obj, path);
                return obj;
            }
            else
            {
                EditorUtility.CopySerialized(obj, asset);
                return (T)asset;
            }
        }

        public static T AddAssetToAsset<T>(T obj, Object target) where T : Object
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(target));
            for (int i = 0; i < assets.Length; ++i)
            {
                if (assets[i].name == obj.name && assets[i].GetType() == obj.GetType())
                {
                    EditorUtility.CopySerialized(obj, assets[i]);
                    return obj;
                }
            }
            AssetDatabase.AddObjectToAsset(obj, target);
            return obj;
        }
    }
}