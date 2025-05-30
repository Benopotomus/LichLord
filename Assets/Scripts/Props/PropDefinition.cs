using UnityEngine;

namespace LichLord.Props
{
    [CreateAssetMenu(fileName = "PropDefinition", menuName = "LichLord/Props/PropDefinition")]
    public class PropDefinition : ScriptableObject
    {
        [Tooltip("Name of the prop type (e.g., Tree, Rock)")]
        public string propName;

        // Optional: Add more properties (e.g., prefab reference, scale)
        [Tooltip("Optional prefab for this prop (for reference)")]
        public GameObject prefab;
    }
}