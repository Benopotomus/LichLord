
using LichLord.Buildables;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "InvasionSpawnPointDefinition", menuName = "LichLord/Invasions/InvasionSpawnPointDefinition")]
    public class InvasionSpawnPointDefinition : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField]
        private GameObject _prefab;
        public GameObject Prefab;
#endif
    }
}
