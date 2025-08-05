using UnityEngine;
using LichLord.Props;
using AYellowpaper.SerializedCollections;

namespace LichLord.World
{
    public class LevelEditor : MonoBehaviour
    {
        [SerializeField] private WorldSettings worldSettings;
        public WorldSettings WorldSettings => worldSettings;

        [SerializeField] private GlobalTables globalTables;
        public GlobalTables GlobalTables => globalTables;

        [SerializedDictionary("Prop Definition", "Marker")]
        public SerializedDictionary<PropDefinition, PropMarker> PropMarkerPrefabs;

        public InvasionSpawnPointMarker InvasionSpawnMarkerPrefab;

        [SerializeField]
        public LevelEditorMarker MarkerPrefab;
    }
}
