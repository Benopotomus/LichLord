using UnityEngine;
using UnityEditor;
using System.IO;
using LichLord.NonPlayerCharacters;

namespace LichLord.Editor
{
    public static class ManeuverConverter
    {
        [MenuItem("Assets/LichLord/Convert to Attack Maneuver", true)]
        private static bool ValidateConvert()
        {
            return Selection.activeObject is NonPlayerCharacterManeuverDefinition &&
                   !(Selection.activeObject is NonPlayerCharacterAttackManeuverDefinition);
        }

        [MenuItem("Assets/LichLord/Convert to Attack Maneuver")]
        private static void ConvertToAttackManeuver()
        {
            var original = Selection.activeObject as NonPlayerCharacterManeuverDefinition;
            if (original == null)
                return;

            string path = AssetDatabase.GetAssetPath(original);

            // 1. Serialize original to JSON
            string json = JsonUtility.ToJson(original);

            // 2. Create new derived object
            var newAsset = ScriptableObject.CreateInstance<NonPlayerCharacterAttackManeuverDefinition>();

            // 3. Overwrite new asset with old JSON
            JsonUtility.FromJsonOverwrite(json, newAsset);

            // 4. Replace asset
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Converted {Path.GetFileName(path)} to NonPlayerCharacterAttackManeuverDefinition");
        }
    }
}
