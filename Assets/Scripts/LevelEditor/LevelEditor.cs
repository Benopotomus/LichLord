using UnityEngine;
using LichLord.Props;

namespace LichLord.World
{
    public class LevelEditor : MonoBehaviour
    {
        [SerializeField] private WorldSettings worldSettings;
        public WorldSettings WorldSettings => worldSettings;

        [SerializeField] private GlobalTables globalTables;
        public GlobalTables GlobalTables => globalTables;
    }
}
