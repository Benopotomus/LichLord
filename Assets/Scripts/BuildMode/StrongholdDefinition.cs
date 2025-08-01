
using LichLord.Buildables;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "StrongholdDefinition", menuName = "LichLord/Strongholds/StrongholdDefinition")]
    public class StrongholdDefinition : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField]
        private Stronghold _prefab;
        public Stronghold Prefab;
#endif
    }
}
