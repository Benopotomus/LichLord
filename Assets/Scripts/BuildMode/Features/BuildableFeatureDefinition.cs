using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(menuName = "LichLord/Buildables/BuildableFeatureDefinition")]
    public class BuildableFeatureDefinition : BuildableDefinition
    {
        [SerializeField]
        private Vector3 _checkBounds;
        public Vector3 CheckBounds => _checkBounds;
    }
}
